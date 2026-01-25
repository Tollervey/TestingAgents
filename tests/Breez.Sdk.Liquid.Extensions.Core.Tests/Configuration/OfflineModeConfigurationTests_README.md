# Offline Mode Configuration Tests - TDD RED Phase Summary

## Test File Location
`C:\Work\ClaudeCode\Spikes\TestingAgents\tests\Breez.Sdk.Liquid.Extensions.Core.Tests\Configuration\OfflineModeConfigurationTests.cs`

## Purpose
This test suite verifies the offline mode configuration requirements for BreezSDK Liquid integrations:

1. **Service Registration**: Verify `AddBreezSdkOffline` registers `OfflineBreezSdkService` not `BreezSdkService`
2. **No Wrapper Registration**: Verify `AddBreezSdkOffline` does NOT register `IBreezSdkWrapper`
3. **Optional Credentials**: Verify ApiKey and Mnemonic are NOT required in offline mode
4. **WorkingDirectory Required**: Verify WorkingDirectory is still required even in offline mode
5. **Simulation Options**: Verify offline simulation options (OfflineMockBalanceSat, OfflineSimulateDelayMs, OfflineSimulateFailureRate) are honored
6. **Mode Switching**: Verify switching between online and offline modes works correctly

## Test Categories

### 1. Offline Mode Service Registration Tests (5 tests)
- ✅ `AddBreezSdkOffline_RegistersOfflineBreezSdkService_NotBreezSdkService` - **SHOULD PASS** (already implemented)
- ✅ `AddBreezSdkOffline_DoesNotRegisterIBreezSdkWrapper` - **SHOULD PASS** (already implemented)
- ✅ `AddBreezSdkOffline_RegistersIBreezSdkService_AsSingleton` - **SHOULD PASS** (already implemented)
- ✅ `AddBreezSdkOffline_RegistersIPaymentRepository` - **SHOULD PASS** (already implemented)
- ✅ `AddBreezSdkOffline_RegistersConcreteBreezSdkWrapper` - **SHOULD PASS** (existing ServiceCollectionExtensions.cs)

### 2. Offline Mode Validation - ApiKey and Mnemonic NOT Required (3 tests)
- **EXPECTED TO FAIL** ❌ `AddBreezSdkOffline_DoesNotRequireApiKey`
- **EXPECTED TO FAIL** ❌ `AddBreezSdkOffline_DoesNotRequireMnemonic`
- **EXPECTED TO FAIL** ❌ `AddBreezSdkOffline_AllowsEmptyApiKeyAndMnemonic`

**Why these will fail**: The `AddBreezSdkOffline` methods do NOT automatically set `options.OfflineMode = true`. The validator checks the `OfflineMode` flag, so if the flag isn't set, it will still require credentials.

**Fix Required**: `ServiceCollectionExtensions.AddBreezSdkOffline` should automatically set `options.OfflineMode = true` after the configure action runs:

```csharp
public static IServiceCollection AddBreezSdkOffline(
    this IServiceCollection services,
    Action<BreezSdkOptions> configureOptions)
{
    ArgumentNullException.ThrowIfNull(services);
    ArgumentNullException.ThrowIfNull(configureOptions);

    // Configure options with forced offline mode
    services.Configure<BreezSdkOptions>(options =>
    {
        configureOptions(options);
        options.OfflineMode = true; // FORCE offline mode
    });

    // ... rest of implementation
}
```

### 3. Offline Mode Validation - WorkingDirectory Still Required (3 tests)
- ✅ `AddBreezSdkOffline_RequiresWorkingDirectory` - **SHOULD PASS** (validator already checks this)
- ✅ `AddBreezSdkOffline_RequiresNonEmptyWorkingDirectory` - **SHOULD PASS** (validator already checks this)
- ✅ `AddBreezSdkOffline_WithValidWorkingDirectory_Succeeds` - **SHOULD PASS** if OfflineMode is set

### 4. Offline Simulation Options Tests (8 tests)
- ✅ `AddBreezSdkOffline_HonorsOfflineMockBalanceSat` - **SHOULD PASS**
- ✅ `AddBreezSdkOffline_DefaultMockBalanceSat_Is100000` - **SHOULD PASS**
- ✅ `AddBreezSdkOffline_HonorsOfflineSimulateDelayMs` - **SHOULD PASS**
- ✅ `AddBreezSdkOffline_DefaultSimulateDelayMs_IsZero` - **SHOULD PASS**
- ✅ `AddBreezSdkOffline_HonorsOfflineSimulateFailureRate` - **SHOULD PASS**
- ✅ `AddBreezSdkOffline_DefaultSimulateFailureRate_IsZero` - **SHOULD PASS**
- ✅ `AddBreezSdkOffline_AllowsMaximumFailureRate` - **SHOULD PASS**
- ✅ `AddBreezSdkOffline_WithAllSimulationOptions_ConfiguresCorrectly` - **SHOULD PASS**

### 5. Mode Switching Tests (5 tests)
- ✅ `SwitchingFromOnlineToOfflineMode_ChangesServiceRegistration` - **SHOULD PASS**
- ✅ `SwitchingFromOnlineToOfflineMode_OnlineRegistersWrapper_OfflineDoesNot` - **SHOULD PASS**
- **EXPECTED TO FAIL** ❌ `AddBreezSdk_WithOfflineModeTrue_StillRequiresApiKeyAndMnemonic` - Actually this test expects NOT to throw, so it **SHOULD PASS**
- **EXPECTED TO FAIL** ❌ `AddBreezSdkOffline_WithConfigurationSection_SetsOfflineModeFlag` - Will fail if OfflineMode isn't auto-set

### 6. Configuration Section Tests (2 tests)
- **EXPECTED TO FAIL** ❌ `AddBreezSdkOffline_WithConfigurationSection_DoesNotRequireCredentials`
- ✅ `AddBreezSdkOffline_WithConfigurationSection_CanStillProvideOptionalCredentials` - **SHOULD PASS** if config explicitly sets OfflineMode=true

### 7. Argument Validation Tests (3 tests)
- ✅ `AddBreezSdkOffline_ThrowsArgumentNullException_WhenServicesIsNull` - **SHOULD PASS**
- ✅ `AddBreezSdkOffline_ThrowsArgumentNullException_WhenConfigureOptionsIsNull` - **SHOULD PASS**
- ✅ `AddBreezSdkOffline_ThrowsArgumentNullException_WhenConfigurationSectionIsNull` - **SHOULD PASS**

### 8. Integration with Repository Tests (1 test)
- ✅ `AddBreezSdkOffline_AllowsCustomRepositoryOverride` - **SHOULD PASS**

## Expected Test Results (TDD RED Phase)

### Tests That Should PASS (22 tests)
Most tests should pass because the infrastructure is already in place.

### Tests That Will FAIL (6 tests)
The following tests will fail because `AddBreezSdkOffline` doesn't automatically set `OfflineMode = true`:

1. `AddBreezSdkOffline_DoesNotRequireApiKey`
2. `AddBreezSdkOffline_DoesNotRequireMnemonic`
3. `AddBreezSdkOffline_AllowsEmptyApiKeyAndMnemonic`
4. `AddBreezSdkOffline_WithConfigurationSection_SetsOfflineModeFlag`
5. `AddBreezSdkOffline_WithConfigurationSection_DoesNotRequireCredentials`
6. `AddBreezSdkOffline_WithValidWorkingDirectory_Succeeds` (might fail if OfflineMode isn't set)

## Implementation Changes Needed (GREEN Phase)

### Change 1: ServiceCollectionExtensions.AddBreezSdkOffline (Action overload)
```csharp
public static IServiceCollection AddBreezSdkOffline(
    this IServiceCollection services,
    Action<BreezSdkOptions> configureOptions)
{
    ArgumentNullException.ThrowIfNull(services);
    ArgumentNullException.ThrowIfNull(configureOptions);

    // Configure options with forced offline mode
    services.Configure<BreezSdkOptions>(options =>
    {
        configureOptions(options);
        options.OfflineMode = true; // FORCE offline mode
    });

    services.AddSingleton<IValidateOptions<BreezSdkOptions>, BreezSdkOptionsValidator>();
    services.AddLogging();
    services.AddSingleton<IBreezSdkService, OfflineBreezSdkService>();
    services.TryAddSingleton<IPaymentRepository, InMemoryPaymentRepository>();

    return services;
}
```

### Change 2: ServiceCollectionExtensions.AddBreezSdkOffline (IConfigurationSection overload)
```csharp
public static IServiceCollection AddBreezSdkOffline(
    this IServiceCollection services,
    IConfigurationSection configurationSection)
{
    ArgumentNullException.ThrowIfNull(services);
    ArgumentNullException.ThrowIfNull(configurationSection);

    // Bind configuration section first
    services.Configure<BreezSdkOptions>(configurationSection);

    // Then force offline mode
    services.Configure<BreezSdkOptions>(options =>
    {
        options.OfflineMode = true; // FORCE offline mode
    });

    services.AddSingleton<IValidateOptions<BreezSdkOptions>, BreezSdkOptionsValidator>();
    services.AddLogging();
    services.AddSingleton<IBreezSdkService, OfflineBreezSdkService>();
    services.TryAddSingleton<IPaymentRepository, InMemoryPaymentRepository>();

    return services;
}
```

## Constitutional Compliance

This test suite enforces:

- **Article III.1 Test-First Imperative**: Tests written BEFORE implementation changes
- **Article III.2 Coverage Requirements**: Comprehensive coverage of offline mode configuration
- **Article III.3 Test Isolation**: Each test is independent, uses fresh service collections
- **Article III.4 Automated Validation**: All tests run via `dotnet test`
- **Article VII.1 Exception Handling**: Tests verify proper exception throwing for invalid configurations

## Running the Tests

Currently, the test project has compilation errors in OTHER test files (SecretsRedactionTests.cs, ConfigurationValidationTests.cs) that prevent running these tests.

Once those are fixed, run:
```bash
dotnet test "C:\Work\ClaudeCode\Spikes\TestingAgents\tests\Breez.Sdk.Liquid.Extensions.Core.Tests\Breez.Sdk.Liquid.Extensions.Core.Tests.csproj" --filter "FullyQualifiedName~OfflineModeConfigurationTests"
```

## Next Steps (TDD GREEN Phase)

1. Fix compilation errors in other test files (or temporarily exclude them)
2. Run OfflineModeConfigurationTests to confirm RED phase
3. Implement the two changes to ServiceCollectionExtensions.cs
4. Re-run tests to confirm GREEN phase
5. Refactor if needed (TDD REFACTOR phase)
