namespace Accounts.Accounts.Features;

public record GetMyAccountQuery(Guid CustomerId) : IQuery<AccountDto>;

internal sealed class GetMyAccountHandler(AccountsDbContext dbContext)
  : IQueryHandler<GetMyAccountQuery, AccountDto>
{
  public async Task<AccountDto> Handle(GetMyAccountQuery query, CancellationToken cancellationToken)
  {
    CustomerAccount? account = await dbContext.CustomerAccounts
      .AsNoTracking()
      .Include(item => item.Addresses)
      .Include(item => item.PaymentMethods)
      .SingleOrDefaultAsync(item => item.Id == query.CustomerId, cancellationToken);

    return account is null
      ? AccountMapping.Empty()
      : AccountMapping.ToDto(account);
  }
}

public record UpdatePreferencesCommand(Guid CustomerId, PreferencesDto Preferences)
  : ICommand<AccountDto>;

public sealed class UpdatePreferencesCommandValidator : AbstractValidator<UpdatePreferencesCommand>
{
  public UpdatePreferencesCommandValidator()
  {
    RuleFor(command => command.Preferences.Locale).NotEmpty().MaximumLength(10);
    RuleFor(command => command.Preferences.Currency).NotEmpty().Length(3);
  }
}

internal sealed class UpdatePreferencesHandler(AccountsDbContext dbContext)
  : ICommandHandler<UpdatePreferencesCommand, AccountDto>
{
  public async Task<AccountDto> Handle(UpdatePreferencesCommand command, CancellationToken cancellationToken)
  {
    CustomerAccount account = await LoadOrCreate(command.CustomerId, cancellationToken);
    PreferencesDto preferences = command.Preferences;
    account.UpdatePreferences(
      preferences.Locale,
      preferences.Currency.ToUpperInvariant(),
      preferences.OrderStatusNotifications,
      preferences.MarketingEmails);
    await dbContext.SaveChangesAsync(cancellationToken);
    return AccountMapping.ToDto(account);
  }

  private async Task<CustomerAccount> LoadOrCreate(Guid customerId, CancellationToken cancellationToken)
  {
    CustomerAccount? account = await dbContext.CustomerAccounts
      .Include(item => item.Addresses)
      .Include(item => item.PaymentMethods)
      .SingleOrDefaultAsync(item => item.Id == customerId, cancellationToken);
    if (account is not null)
    {
      return account;
    }

    account = CustomerAccount.Create(customerId);
    dbContext.CustomerAccounts.Add(account);
    return account;
  }
}

public record AddAddressCommand(Guid CustomerId, SaveAddressDto Address)
  : ICommand<SavedAddressDto>;

public record UpdateAddressCommand(Guid CustomerId, Guid AddressId, SaveAddressDto Address)
  : ICommand<UpdateAddressResult>;

public record UpdateAddressResult(SavedAddressDto? Address);

public record DeleteAddressCommand(Guid CustomerId, Guid AddressId)
  : ICommand<bool>;

public sealed class AddAddressCommandValidator : AbstractValidator<AddAddressCommand>
{
  public AddAddressCommandValidator() => AddRules(RuleFor(command => command.Address));

  internal static void AddRules(IRuleBuilderInitial<AddAddressCommand, SaveAddressDto> rule)
  {
    rule.SetValidator(new SaveAddressValidator());
  }
}

public sealed class UpdateAddressCommandValidator : AbstractValidator<UpdateAddressCommand>
{
  public UpdateAddressCommandValidator()
  {
    RuleFor(command => command.AddressId).NotEmpty();
    RuleFor(command => command.Address).SetValidator(new SaveAddressValidator());
  }
}

public sealed class SaveAddressValidator : AbstractValidator<SaveAddressDto>
{
  public SaveAddressValidator()
  {
    RuleFor(address => address.Label).NotEmpty().MaximumLength(40);
    RuleFor(address => address.FirstName).NotEmpty().MaximumLength(50);
    RuleFor(address => address.LastName).NotEmpty().MaximumLength(50);
    RuleFor(address => address.Email).NotEmpty().EmailAddress().MaximumLength(100);
    RuleFor(address => address.Phone).NotEmpty().MaximumLength(30);
    RuleFor(address => address.AddressLine1).NotEmpty().MaximumLength(180);
    RuleFor(address => address.AddressLine2).MaximumLength(180);
    RuleFor(address => address.City).NotEmpty().MaximumLength(80);
    RuleFor(address => address.State).NotEmpty().MaximumLength(80);
    RuleFor(address => address.PostalCode).NotEmpty().MaximumLength(20);
    RuleFor(address => address.CountryCode).NotEmpty().Length(2);
  }
}

internal sealed class AddAddressHandler(AccountsDbContext dbContext)
  : ICommandHandler<AddAddressCommand, SavedAddressDto>
{
  public async Task<SavedAddressDto> Handle(AddAddressCommand command, CancellationToken cancellationToken)
  {
    CustomerAccount? account = await dbContext.CustomerAccounts
      .Include(item => item.Addresses)
      .SingleOrDefaultAsync(item => item.Id == command.CustomerId, cancellationToken);
    if (account is null)
    {
      account = CustomerAccount.Create(command.CustomerId);
      dbContext.CustomerAccounts.Add(account);
    }

    SavedAddress address = account.AddAddress(AccountMapping.ToData(command.Address));
  dbContext.SavedAddresses.Add(address);
    await dbContext.SaveChangesAsync(cancellationToken);
    return AccountMapping.ToDto(address);
  }
}

internal sealed class UpdateAddressHandler(AccountsDbContext dbContext)
  : ICommandHandler<UpdateAddressCommand, UpdateAddressResult>
{
  public async Task<UpdateAddressResult> Handle(
    UpdateAddressCommand command,
    CancellationToken cancellationToken)
  {
    CustomerAccount? account = await dbContext.CustomerAccounts
      .Include(item => item.Addresses)
      .SingleOrDefaultAsync(item => item.Id == command.CustomerId, cancellationToken);
    if (account is null || !account.UpdateAddress(command.AddressId, AccountMapping.ToData(command.Address)))
    {
      return new UpdateAddressResult(null);
    }

    await dbContext.SaveChangesAsync(cancellationToken);
    return new UpdateAddressResult(
      AccountMapping.ToDto(account.Addresses.Single(item => item.Id == command.AddressId)));
  }
}

internal sealed class DeleteAddressHandler(AccountsDbContext dbContext)
  : ICommandHandler<DeleteAddressCommand, bool>
{
  public async Task<bool> Handle(DeleteAddressCommand command, CancellationToken cancellationToken)
  {
    CustomerAccount? account = await dbContext.CustomerAccounts
      .Include(item => item.Addresses)
      .SingleOrDefaultAsync(item => item.Id == command.CustomerId, cancellationToken);
    if (account is null || !account.RemoveAddress(command.AddressId))
    {
      return false;
    }

    await dbContext.SaveChangesAsync(cancellationToken);
    return true;
  }
}

public record AddPaymentMethodCommand(Guid CustomerId, SavePaymentMethodDto PaymentMethod)
  : ICommand<SavedPaymentMethodDto>;

public record DeletePaymentMethodCommand(Guid CustomerId, Guid PaymentMethodId)
  : ICommand<bool>;

public record SetDefaultPaymentMethodCommand(Guid CustomerId, Guid PaymentMethodId)
  : ICommand<SetDefaultPaymentMethodResult>;

public record SetDefaultPaymentMethodResult(SavedPaymentMethodDto? PaymentMethod);

public sealed class SavePaymentMethodValidator : AbstractValidator<SavePaymentMethodDto>
{
  public SavePaymentMethodValidator()
  {
    RuleFor(paymentMethod => paymentMethod.Label).NotEmpty().MaximumLength(40);
    RuleFor(paymentMethod => paymentMethod.CardholderName).NotEmpty().MaximumLength(100);
    RuleFor(paymentMethod => paymentMethod.Brand).NotEmpty().MaximumLength(30);
    RuleFor(paymentMethod => paymentMethod.Last4).Matches("^[0-9]{4}$");
    RuleFor(paymentMethod => paymentMethod.Expiration).NotEmpty().MaximumLength(10);
    RuleFor(paymentMethod => paymentMethod.Token).NotEmpty().MaximumLength(200);
  }
}

public sealed class AddPaymentMethodCommandValidator : AbstractValidator<AddPaymentMethodCommand>
{
  public AddPaymentMethodCommandValidator() => RuleFor(command => command.PaymentMethod).SetValidator(new SavePaymentMethodValidator());
}

internal sealed class AddPaymentMethodHandler(AccountsDbContext dbContext)
  : ICommandHandler<AddPaymentMethodCommand, SavedPaymentMethodDto>
{
  public async Task<SavedPaymentMethodDto> Handle(AddPaymentMethodCommand command, CancellationToken cancellationToken)
  {
    CustomerAccount? account = await dbContext.CustomerAccounts
      .Include(item => item.PaymentMethods)
      .SingleOrDefaultAsync(item => item.Id == command.CustomerId, cancellationToken);
    if (account is null)
    {
      account = CustomerAccount.Create(command.CustomerId);
      dbContext.CustomerAccounts.Add(account);
    }

    SavedPaymentMethod paymentMethod = account.AddPaymentMethod(AccountMapping.ToData(command.PaymentMethod));
    dbContext.SavedPaymentMethods.Add(paymentMethod);
    await dbContext.SaveChangesAsync(cancellationToken);
    return AccountMapping.ToDto(paymentMethod);
  }
}

internal sealed class DeletePaymentMethodHandler(AccountsDbContext dbContext)
  : ICommandHandler<DeletePaymentMethodCommand, bool>
{
  public async Task<bool> Handle(DeletePaymentMethodCommand command, CancellationToken cancellationToken)
  {
    CustomerAccount? account = await dbContext.CustomerAccounts
      .Include(item => item.PaymentMethods)
      .SingleOrDefaultAsync(item => item.Id == command.CustomerId, cancellationToken);
    if (account is null || !account.RemovePaymentMethod(command.PaymentMethodId))
    {
      return false;
    }

    await dbContext.SaveChangesAsync(cancellationToken);
    return true;
  }
}

internal sealed class SetDefaultPaymentMethodHandler(AccountsDbContext dbContext)
  : ICommandHandler<SetDefaultPaymentMethodCommand, SetDefaultPaymentMethodResult>
{
  public async Task<SetDefaultPaymentMethodResult> Handle(SetDefaultPaymentMethodCommand command, CancellationToken cancellationToken)
  {
    CustomerAccount? account = await dbContext.CustomerAccounts
      .Include(item => item.PaymentMethods)
      .SingleOrDefaultAsync(item => item.Id == command.CustomerId, cancellationToken);
    if (account is null || !account.SetDefaultPaymentMethod(command.PaymentMethodId))
    {
      return new SetDefaultPaymentMethodResult(null);
    }

    await dbContext.SaveChangesAsync(cancellationToken);
    return new SetDefaultPaymentMethodResult(AccountMapping.ToDto(account.PaymentMethods.Single(item => item.Id == command.PaymentMethodId)));
  }
}

internal static class AccountMapping
{
  public static AccountDto Empty() => new(
    new PreferencesDto("en-US", "USD", true, false),
    [],
    []);

  public static AccountDto ToDto(CustomerAccount account) => new(
    new PreferencesDto(
      account.Locale,
      account.Currency,
      account.OrderStatusNotifications,
      account.MarketingEmails),
    account.Addresses.Select(ToDto).ToArray(),
    account.PaymentMethods.Select(ToDto).ToArray());

  public static SavedAddressDto ToDto(SavedAddress address) => new(
    address.Id,
    address.Label,
    address.FirstName,
    address.LastName,
    address.Email,
    address.Phone,
    address.AddressLine1,
    address.AddressLine2,
    address.City,
    address.State,
    address.PostalCode,
    address.CountryCode,
    address.IsDefaultShipping,
    address.IsDefaultBilling);

  public static AddressData ToData(SaveAddressDto address) => new(
    address.Label,
    address.FirstName,
    address.LastName,
    address.Email,
    address.Phone,
    address.AddressLine1,
    address.AddressLine2,
    address.City,
    address.State,
    address.PostalCode,
    address.CountryCode,
    address.IsDefaultShipping,
    address.IsDefaultBilling);

  public static SavedPaymentMethodDto ToDto(SavedPaymentMethod paymentMethod) => new(
    paymentMethod.Id,
    paymentMethod.Label,
    paymentMethod.CardholderName,
    paymentMethod.Brand,
    paymentMethod.Last4,
    paymentMethod.Expiration,
    paymentMethod.Token,
    paymentMethod.IsDefault);

  public static PaymentMethodData ToData(SavePaymentMethodDto paymentMethod) => new(
    paymentMethod.Label,
    paymentMethod.CardholderName,
    paymentMethod.Brand,
    paymentMethod.Last4,
    paymentMethod.Expiration,
    paymentMethod.Token,
    paymentMethod.IsDefault);
}
