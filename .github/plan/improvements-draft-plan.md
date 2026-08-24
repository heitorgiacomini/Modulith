1. set all constructos of classes inside models to private and create static factory methods for them if needed 
2. rename "Models" folder inside module to "Domain"?
3. ICurrentTenant is registered from shared module, could we register ICurrentUser too? what can we do reduce code in infrastructure folder inside api project?
4. There is lots of hardcoded Autorization requirements inside api project, should move them to each module and use them from there. example existing code in program.cs:   options.FallbackPolicy = new AuthorizationPolicyBuilder()
    .RequireAuthenticatedUser()
    .AddRequirements(new CurrentTenantRequirement())
    .AddRequirements(new OrganizationRoleRequirement("customer", "admin"))
    .Build();
  options.AddPolicy(TenantAuthorizationPolicies.Admin, policy => policy
    .RequireAuthenticatedUser()
    .AddRequirements(new CurrentTenantRequirement())
    .AddRequirements(new OrganizationRoleRequirement("admin")));
});
5. Autorization should be handed in domain layer and not in Endpoint.cs like: .RequireAuthorization(TenantAuthorizationPolicies.Admin). what do you think?
7. Ensure ever Enpointcs clas return dto instead of domain object.
8. use Mapperly instead of mapster, do not use hand made onversion like: 
\src\Modules\Accounts\Accounts\Mapping\AccountMapping.cs. set Mapperly as a shared reference and use it in all modules.
9. improve shared.md description and explain ther libraries used.
10. create a module.md file explaining how it works



