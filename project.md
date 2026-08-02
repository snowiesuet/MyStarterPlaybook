# MyStartPlaybook
Version: 0.2 (Architecture Phase)
Platform: NinjaTrader 8
Language: C#
Status: Active Development

---

# Objective

Build a reusable, modular order management engine that can be reused across multiple entry strategies.

Current priority is **correct architecture**, not profitability.

Do NOT optimize parameters until the order manager is verified.

---

# Design Philosophy

Separate the strategy into independent modules.

```
Entry
    ↓
Order Manager
    ↓
Exit Manager
```

Each module must be swappable.

Examples:

Entry

- EMA Cross
- EMA + VWAP
- ORB
- Pullback
- Bookmap (future)

Order Manager

- ATR Stop
- ATR Trail
- Break-even
- Profit Lock
- Partial Exit

Exit

- ATR Trail
- EMA Exit
- Time Exit
- Swing Exit

Changing one module should not require modifying the others.

---

# Current Entry Logic

Indicators

```
EMA Fast = 10
EMA Slow = 25
ATR = 14
```

Long

```csharp
CrossAbove(emaFast, emaSlow, 1)
```

Short

```csharp
CrossBelow(emaFast, emaSlow, 1)
```

Only one position at a time.

No pyramiding.

---

# Current Strategy Lifecycle

## OnStateChange()

Responsibilities

- Initialize defaults
- Create indicators
- Add indicators to chart

Nothing else.

---

## OnBarUpdate()

Responsibilities ONLY

- Detect entry signals
- Future:
    - Update trailing stop
    - Update break-even
    - Profit locking

Should NEVER perform large initialization.

---

## OnExecutionUpdate()

Responsibilities

Runs when an order is actually filled.

Responsible for:

- Capture actual fill price
- Calculate ATR values
- Initialize trade state
- Submit initial stop (planned)

This method is the source of truth for trade initialization.

---

# Current Variables

Indicators

```csharp
EMA emaFast;
EMA emaSlow;
ATR atr;
```

Trade State

```csharp
double entryPrice;

double initialStop;

double trailActivation;

double trailingStop;

bool stopPlaced;

bool isTrailing;
```

Strategy Parameters

```csharp
Fast

Slow

ATRPeriod

StopATR

TrailActivationATR

TrailATR
```

---

# ATR Calculations

Long

```
initialStop =
EntryPrice
-
ATR * StopATR
```

```
trailActivation =
EntryPrice
+
ATR * TrailActivationATR
```

Short

```
initialStop =
EntryPrice
+
ATR * StopATR
```

```
trailActivation =
EntryPrice
-
ATR * TrailActivationATR
```

Current defaults

```
ATRPeriod = 14

StopATR = 2

TrailActivationATR = 1

TrailATR = 2.5
```

---

# Current Verified Functionality

Verified

✅ Strategy compiles

✅ EMA plots

✅ ATR plots

✅ EMA crossover entries

✅ OnExecutionUpdate fires

✅ Actual fill price captured

✅ ATR calculations correct

Example debug

```
LONG

Entry Price

ATR

Initial Stop

Trail Activation
```

The calculations themselves have been verified.

---

# Current Problem

Stop submission.

Two methods tested.

## Attempt 1

```
ExitLongStopMarket()
ExitShortStopMarket()
```

Behavior inconsistent.

Needs redesign.

---

## Attempt 2

```
SetStopLoss()
```

Observed issue

Example

```
Entry

28381.50

Expected Stop

28404.40

Actual Stop Execution

28381.50
```

Stop executed at entry price instead of expected stop.

Conclusion:

Using SetStopLoss after execution is likely incorrect for this architecture.

Need to redesign stop submission.

---

# Current Belief

Because stop price depends on actual fill price:

```
Signal

↓

Fill

↓

Calculate Stop

↓

Submit Stop
```

The strategy should likely use

```
ExitLongStopMarket()

ExitShortStopMarket()
```

submitted immediately after execution rather than SetStopLoss().

Requires further verification.

---

# Future Order Flow

Desired lifecycle

```
EMA Cross

↓

Enter

↓

Order Filled

↓

OnExecutionUpdate()

↓

Capture Fill Price

↓

Calculate ATR Stop

↓

Submit Initial Stop

↓

OnBarUpdate()

↓

Monitor Position

↓

If price reaches trail activation

↓

Enable trailing

↓

Move stop only toward profit

↓

Exit
```

---

# Trailing Logic (Planned)

Long

Activation

```
High >= trailActivation
```

Once activated

```
newStop =
Close
-
ATR * TrailATR
```

Only move if

```
newStop > trailingStop
```

Never move stop backwards.

---

Short

Mirror image.

Activation

```
Low <= trailActivation
```

Once activated

```
newStop =
Close
+
ATR * TrailATR
```

Only move if

```
newStop < trailingStop
```

---

# Debug Philosophy

Every important event must print.

Required debug output

```
ENTRY

Price

ATR

Initial Stop

Trail Activation
```

Later

```
TRAIL ACTIVATED
```

Later

```
STOP UPDATED

Old

↓

New
```

Later

```
EXIT

Reason

Price

PnL
```

Never debug by reading code.

Always verify behavior through logs.

---

# Development Rules

DO

- Keep methods small.
- Single responsibility per method.
- Modular architecture.
- Reusable order manager.
- Prefer explicit state over implicit behavior.
- Verify each feature independently.

DO NOT

- Optimize before correctness.
- Add partial exits yet.
- Add profit targets yet.
- Mix entry logic with order management.
- Introduce multiple responsibilities into one method.

---

# Coding Style

Preferred

```
OnStateChange()

↓

OnBarUpdate()

↓

OnExecutionUpdate()
```

Avoid

- giant methods
- nested logic
- duplicated calculations

---

# Long-Term Goal

Final architecture

```
Entry Module
    EMA
    VWAP
    ORB
    Pullback

↓

Order Manager
    ATR Stop
    ATR Trail
    Break-even
    Profit Lock
    Partial Exit

↓

Exit Module
    ATR
    EMA
    Time
    Swing
```

Entry methods should be interchangeable without modifying the order manager.

---

# Current Task

Primary blocker

Redesign initial stop submission.

Investigate:

- Best timing for stop submission
- Correct NinjaTrader managed/unmanaged order usage
- Strategy Analyzer vs Live consistency

Do NOT continue implementing trailing logic until initial stop behavior is fully verified.