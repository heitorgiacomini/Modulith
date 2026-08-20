import { Injectable } from '@angular/core';
import { FilterMetadata, SortMeta } from 'primeng/api';

type GraphqlFilter = Record<string, unknown>;

type GraphqlFilterOperation =
  | 'contains'
  | 'endsWith'
  | 'eq'
  | 'gt'
  | 'gte'
  | 'in'
  | 'lt'
  | 'lte'
  | 'ncontains'
  | 'nendsWith'
  | 'neq'
  | 'nin'
  | 'nstartsWith'
  | 'startsWith';

export interface GraphqlLazyLoadEvent {
  first?: number | null;
  rows?: number | null;
  sortField?: string | string[] | null;
  sortOrder?: number | null;
  multiSortMeta?: Array<SortMeta | null | undefined> | null;
  filters?: Record<string, FilterMetadata | FilterMetadata[] | undefined> | null;
}

export interface GraphqlSelectionNode {
  name: string;
  fields: Array<string | GraphqlSelectionNode>;
}

export interface GraphqlQueryDefinition {
  operationName: string;
  variableDefinitions?: Record<string, string>;
  rootField: string;
  rootArguments?: Record<string, string>;
  selection: Array<string | GraphqlSelectionNode>;
}

export interface GraphqlCollectionVariables {
  skip: number;
  take: number;
  where: Record<string, unknown> | null;
  order: Array<Record<string, unknown>> | null;
}

@Injectable({
  providedIn: 'root'
})
export class GraphqlQueryBuilderService {
  buildQuery(definition: GraphqlQueryDefinition): string {
    const variableDefinitions = this.buildVariableDefinitions(definition.variableDefinitions);
    const rootArguments = this.buildRootArguments(definition.rootArguments);
    const selection = this.buildSelection(definition.selection);

    return `query ${definition.operationName}${variableDefinitions} { ${definition.rootField}${rootArguments} { ${selection} } }`;
  }

  buildCollectionVariables(event: GraphqlLazyLoadEvent, defaultPageSize: number): GraphqlCollectionVariables {
    const take = event.rows && event.rows > 0 ? event.rows : defaultPageSize;
    const skip = event.first && event.first > 0 ? event.first : 0;

    return {
      skip,
      take,
      where: this.createWhere(event.filters),
      order: this.createOrder(event)
    };
  }

  private buildVariableDefinitions(variableDefinitions?: Record<string, string>): string {
    if (!variableDefinitions || Object.keys(variableDefinitions).length === 0) {
      return '';
    }

    const definitions = Object.entries(variableDefinitions)
      .map(([name, type]) => `$${name}: ${type}`)
      .join(', ');
    return `(${definitions})`;
  }

  private buildRootArguments(rootArguments?: Record<string, string>): string {
    if (!rootArguments || Object.keys(rootArguments).length === 0) {
      return '';
    }

    const args = Object.entries(rootArguments)
      .map(([name, value]) => `${name}: ${value}`)
      .join(', ');
    return `(${args})`;
  }

  private buildSelection(selection: Array<string | GraphqlSelectionNode>): string {
    return selection
      .map(node => {
        if (typeof node === 'string') {
          return node;
        }

        return `${node.name} { ${this.buildSelection(node.fields)} }`;
      })
      .join(' ');
  }

  private createWhere(filters: GraphqlLazyLoadEvent['filters']): GraphqlFilter | null {
    if (!filters) {
      return null;
    }

    const expressions = Object.entries(filters)
      .map(([field, metadata]) => this.createPropertyFilter(field, metadata))
      .filter((expression): expression is GraphqlFilter => expression !== null);

    if (expressions.length === 0) {
      return null;
    }

    return expressions.length === 1 ? expressions[0] : { and: expressions };
  }

  private createPropertyFilter(
    field: string,
    metadata: FilterMetadata | FilterMetadata[] | undefined
  ): GraphqlFilter | null {
    const constraints = Array.isArray(metadata) ? metadata : metadata ? [metadata] : [];
    const expressions = constraints
      .map(constraint => ({
        expression: this.createFilterExpression(field, constraint),
        operator: constraint.operator
      }))
      .filter(
        (constraint): constraint is { expression: GraphqlFilter; operator: string | undefined } =>
          constraint.expression !== null
      );

    if (expressions.length === 0) {
      return null;
    }

    return expressions.slice(1).reduce(
      (combined, constraint, index) => ({
        [this.toLogicalOperator(expressions[index].operator ?? constraint.operator)]: [
          combined,
          constraint.expression
        ]
      }),
      expressions[0].expression
    );
  }

  private createFilterExpression(field: string, filter: FilterMetadata): GraphqlFilter | null {
    if (!filter.matchMode || this.isEmptyFilterValue(filter.value)) {
      return null;
    }

    const operation = this.toGraphqlFilterOperation(filter.matchMode);
    if (!operation) {
      return null;
    }

    return this.createNestedFilter(field, operation, this.normalizeFilterValue(filter.value, filter.matchMode));
  }

  private createNestedFilter(
    field: string,
    operation: GraphqlFilterOperation,
    value: unknown
  ): GraphqlFilter | null {
    const path = field.split('.').filter(Boolean);
    const leafField = path.pop();
    if (!leafField) {
      return null;
    }

    let expression: GraphqlFilter = { [leafField]: { [operation]: value } };
    for (const segment of path.reverse()) {
      expression = { [segment]: expression };
    }

    return expression;
  }

  private toGraphqlFilterOperation(matchMode: string): GraphqlFilterOperation | null {
    switch (matchMode.toLowerCase().split(':')[0]) {
      case 'contains':
        return 'contains';
      case 'notcontains':
      case 'ncontains':
        return 'ncontains';
      case 'startswith':
        return 'startsWith';
      case 'notstartswith':
      case 'nstartswith':
        return 'nstartsWith';
      case 'endswith':
        return 'endsWith';
      case 'notendswith':
      case 'nendswith':
        return 'nendsWith';
      case 'eq':
      case 'equals':
      case 'dateis':
        return 'eq';
      case 'ne':
      case 'notequals':
      case 'dateisnot':
        return 'neq';
      case 'gt':
      case 'greaterthan':
      case 'dateafter':
        return 'gt';
      case 'ge':
      case 'greaterthanorequalto':
        return 'gte';
      case 'lt':
      case 'lessthan':
        return 'lt';
      case 'le':
      case 'lessthanorequalto':
      case 'datebefore':
        return 'lte';
      case 'in':
        return 'in';
      case 'nin':
      case 'notin':
        return 'nin';
      default:
        return null;
    }
  }

  private createOrder(event: GraphqlLazyLoadEvent): Array<Record<string, unknown>> | null {
    const multiSort = event.multiSortMeta?.filter((sort): sort is SortMeta => Boolean(sort?.field));
    if (multiSort?.length) {
      return multiSort.map(sort => this.createNestedSort(sort.field, sort.order));
    }

    const sortField = Array.isArray(event.sortField) ? event.sortField[0] : event.sortField;
    return sortField ? [this.createNestedSort(sortField, event.sortOrder ?? undefined)] : null;
  }

  private createNestedSort(field: string, order: number | undefined): Record<string, unknown> {
    const path = field.split('.').filter(Boolean);
    const leafField = path.pop();
    if (!leafField) {
      return {};
    }

    let sort: Record<string, unknown> = { [leafField]: order && order > 0 ? 'ASC' : 'DESC' };
    for (const segment of path.reverse()) {
      sort = { [segment]: sort };
    }

    return sort;
  }

  private toLogicalOperator(operator: string | undefined): 'and' | 'or' {
    return operator?.toLowerCase() === 'or' ? 'or' : 'and';
  }

  private normalizeFilterValue(value: unknown, matchMode: string): unknown {
    if (value instanceof Date) {
      return matchMode.toLowerCase().startsWith('date')
        ? value.toISOString().split('T')[0]
        : value.toISOString();
    }

    return Array.isArray(value)
      ? value.map(item => this.normalizeFilterValue(item, matchMode))
      : value;
  }

  private isEmptyFilterValue(value: unknown): boolean {
    return value === '' || value === null || value === undefined;
  }
}
