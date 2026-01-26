# Reconnection Tests - Implementation Requirements

## Test File
**Location**: `C:\Work\ClaudeCode\Spikes\TestingAgents\tests\Breez.Sdk.Liquid.Extensions.Core.Tests\Infrastructure\ReconnectionTests.cs`

## Status
**RED PHASE**: Tests written and failing (173 compilation errors as expected)

## Required Interface Changes

### IBreezSdkWrapper Interface Additions
File: `src/Breez.Sdk.Liquid.Extensions.Core/Abstractions/IBreezSdkWrapper.cs`

```csharp
public interface IBreezSdkWrapper : IAsyncDisposable
{
    // Existing members
    bool IsConnected { get; }
    Task ConnectAsync(CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);
    // ... other existing members ...

    // NEW MEMBERS FOR RECONNECTION

    /// <summary>
    /// Gets the current connection state.
    /// </summary>
    ConnectionState State { get; }

    /// <summary>
    /// Event raised when connection state changes.
    /// </summary>
    event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;

    /// <summary>
    /// Attempts to reconnect to the SDK with exponential backoff.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if reconnection succeeded, false otherwise.</returns>
    Task<bool> TryReconnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets whether a reconnection attempt can be made based on current state.
    /// Returns false if already reconnecting, in failed state, or disposed.
    /// </summary>
    bool CanAttemptReconnect { get; }
}
```

### New Types Required

#### ConnectionState Enum
```csharp
/// <summary>
/// Represents the connection state of the SDK wrapper.
/// </summary>
public enum ConnectionState
{
    /// <summary>SDK is disconnected.</summary>
    Disconnected,

    /// <summary>Initial connection in progress.</summary>
    Connecting,

    /// <summary>SDK is connected and operational.</summary>
    Connected,

    /// <summary>Reconnection attempt in progress.</summary>
    Reconnecting,

    /// <summary>Connection failed after max reconnection attempts.</summary>
    Failed
}
```

#### ConnectionStateChangedEventArgs
```csharp
/// <summary>
/// Event args for connection state changes.
/// </summary>
public class ConnectionStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// The previous connection state.
    /// </summary>
    public ConnectionState OldState { get; init; }

    /// <summary>
    /// The new connection state.
    /// </summary>
    public ConnectionState NewState { get; init; }

    /// <summary>
    /// Exception that caused the state change, if any.
    /// </summary>
    public Exception? Exception { get; init; }
}
```

#### ReconnectionOptions Configuration Class
File: `src/Breez.Sdk.Liquid.Extensions.Core/Configuration/ReconnectionOptions.cs`

```csharp
/// <summary>
/// Configuration for automatic reconnection behavior.
/// </summary>
public class ReconnectionOptions
{
    /// <summary>
    /// Maximum number of reconnection attempts before entering Failed state.
    /// Default: 5
    /// </summary>
    public int MaxAttempts { get; set; } = 5;

    /// <summary>
    /// Initial delay in milliseconds before first reconnection attempt.
    /// Default: 1000ms (1 second)
    /// </summary>
    public int InitialDelayMs { get; set; } = 1000;

    /// <summary>
    /// Maximum delay in milliseconds between reconnection attempts.
    /// Default: 30000ms (30 seconds)
    /// </summary>
    public int MaxDelayMs { get; set; } = 30000;

    /// <summary>
    /// Multiplier for exponential backoff calculation.
    /// Each retry delay = previous delay * BackoffMultiplier (capped at MaxDelayMs).
    /// Default: 2.0
    /// </summary>
    public double BackoffMultiplier { get; set; } = 2.0;
}
```

#### Update BreezSdkOptions
File: `src/Breez.Sdk.Liquid.Extensions.Core/Configuration/BreezSdkOptions.cs`

Add property:
```csharp
/// <summary>
/// Reconnection configuration.
/// </summary>
public ReconnectionOptions Reconnection { get; set; } = new();
```

## Implementation Requirements for BreezSdkWrapper

### State Management
1. Add private field: `private ConnectionState _state = ConnectionState.Disconnected;`
2. Add property: `public ConnectionState State => _state;`
3. Add event: `public event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;`
4. Implement state transition logic with event firing

### Reconnection Logic
1. Implement `TryReconnectAsync()` method:
   - Check if reconnection is possible (CanAttemptReconnect)
   - Transition to Reconnecting state
   - Implement exponential backoff retry loop
   - Respect MaxAttempts configuration
   - Preserve event callbacks across reconnections
   - Transition to Connected on success or Failed on max attempts
   - Return true/false based on success

2. Implement `CanAttemptReconnect` property:
   - Return false if State is Reconnecting or Failed
   - Return false if disposed
   - Return true otherwise

### Connection State Transitions
Update existing methods to manage state:

#### ConnectAsync()
- Transition: Disconnected → Connecting
- On success: Connecting → Connected
- On failure: Connecting → Failed (or Disconnected)

#### DisconnectAsync()
- Transition: (any) → Disconnected
- Cancel any ongoing reconnection attempts

#### Operations (PrepareReceivePaymentAsync, GetWalletInfoAsync, etc.)
- When state is Reconnecting: Wait for reconnection to complete
- When state is Failed: Throw ConnectionException immediately
- When state is Connected: Proceed normally

### Event Callback Preservation
- Store event callback in field: `private Action<SdkEvent>? _eventCallback;`
- On reconnection: Re-register the stored callback with SDK
- Ensure callback survives multiple reconnection cycles

### Concurrency Protection
- Use lock or SemaphoreSlim to prevent concurrent reconnection attempts
- Ensure only one reconnection can execute at a time
- Subsequent calls to TryReconnectAsync while reconnecting should wait

## Test Coverage

### Test Categories
1. **Connection State Tracking** (7 tests)
   - State property values across lifecycle
   - State transitions during operations

2. **ConnectionStateChanged Events** (6 tests)
   - Event firing on state transitions
   - Correct old/new state values
   - Exception included on failures

3. **Automatic Reconnection** (8 tests)
   - Successful reconnection
   - Failure after max attempts
   - Exponential backoff timing
   - Cancellation support
   - Already connected handling

4. **CanAttemptReconnect Property** (6 tests)
   - True when disconnected/connected
   - False when reconnecting/failed/disposed

5. **Operation Behavior During Reconnection** (4 tests)
   - Operations wait during reconnection
   - Operations fail fast when failed

6. **Event Callback Preservation** (4 tests)
   - Callbacks survive reconnection
   - Multiple reconnection cycles
   - Callback replacement

7. **Edge Cases and Error Handling** (4 tests)
   - Concurrent reconnection attempts
   - Dispose during reconnection
   - Already disposed errors
   - Disconnection during reconnection

**Total: 39 comprehensive tests**

## Next Steps (Implementation Phase)

1. Create `ConnectionState` enum in `Abstractions/` folder
2. Create `ConnectionStateChangedEventArgs` class in `Abstractions/` folder
3. Create `ReconnectionOptions` class in `Configuration/` folder
4. Update `BreezSdkOptions` to include `Reconnection` property
5. Update `IBreezSdkWrapper` interface with new members
6. Implement reconnection logic in `BreezSdkWrapper`:
   - State management
   - Event firing
   - TryReconnectAsync with exponential backoff
   - CanAttemptReconnect property
   - Operation queueing/waiting during reconnection
   - Event callback preservation
7. Run tests to verify GREEN phase
8. Refactor if needed (REFACTOR phase)

## Validation Checklist

After implementation:
- [ ] All 39 tests pass
- [ ] No compilation errors
- [ ] Code coverage >= 80% for new code
- [ ] State transitions logged appropriately
- [ ] Exponential backoff timing verified
- [ ] Concurrent reconnection handling verified
- [ ] Event callback preservation verified
- [ ] Integration tests added for real SDK reconnection scenarios
