Feature: Reconcile mutable Upstash Redis settings

  Scenario: Explicit matching settings do not call provider mutations
    Given the Upstash reconcile target database has read regions "eu-west-2", plan "payg", budget 360, and eviction enabled
    When Upstash Redis reconciliation runs with read regions "eu-west-2", plan "payg", budget 360, and eviction enabled
    Then Upstash Redis reconciliation succeeds
    And the Upstash reconcile provider recorded no mutation calls

  Scenario: Pay-as-you-go provider plan aliases do not call provider mutations
    Given the Upstash reconcile target database has read regions "eu-west-2", plan "paid", budget 360, and eviction enabled
    When Upstash Redis reconciliation runs with only plan "payg"
    Then Upstash Redis reconciliation succeeds
    And the Upstash reconcile provider recorded no mutation calls

  Scenario: Mutable settings are reconciled in deterministic order
    Given the Upstash reconcile target database has read regions "eu-west-1", plan "free", budget 100, and eviction disabled
    When Upstash Redis reconciliation runs with read regions "eu-west-2", plan "payg", budget 360, and eviction enabled
    Then Upstash Redis reconciliation succeeds
    And the Upstash reconcile provider recorded mutation calls in order:
      | mutation     |
      | read regions |
      | plan         |
      | budget       |
      | eviction     |
    And the Upstash reconcile target database has read regions "eu-west-2", plan "payg", budget 360, and eviction enabled

  Scenario Outline: Each mutable setting can be reconciled on its own
    Given the Upstash reconcile target database has read regions "eu-west-1", plan "free", budget 100, and eviction disabled
    When Upstash Redis reconciliation runs with only <Setting> set to "<Value>"
    Then Upstash Redis reconciliation succeeds
    And the Upstash reconcile provider recorded mutation calls in order:
      | mutation   |
      | <Mutation> |
    And the Upstash reconcile target database has read regions "<ReadRegions>", plan "<Plan>", budget <Budget>, and eviction <Eviction>

    Examples:
      | Setting      | Value     | Mutation     | ReadRegions | Plan | Budget | Eviction |
      | read regions | eu-west-2 | read regions | eu-west-2   | free | 100    | disabled |
      | plan         | payg      | plan         | eu-west-1   | payg | 100    | disabled |
      | budget       | 360       | budget       | eu-west-1   | free | 360    | disabled |
      | eviction     | true      | eviction     | eu-west-1   | free | 100    | enabled  |

  Scenario: Only explicit desired settings are enforced
    Given the Upstash reconcile target database has read regions "eu-west-1", plan "free", budget 100, and eviction disabled
    When Upstash Redis reconciliation runs with only plan "payg"
    Then Upstash Redis reconciliation succeeds
    And the Upstash reconcile provider recorded mutation calls in order:
      | mutation |
      | plan     |
    And the Upstash reconcile target database has read regions "eu-west-1", plan "payg", budget 100, and eviction disabled

  Scenario: Fixed plan reconciliation compares provider disk threshold
    Given the Upstash reconcile target database has read regions "eu-west-1", coarse plan "pro", fixed plan "fixed_250mb", budget 100, and eviction disabled
    When Upstash Redis reconciliation runs with only plan "fixed_250mb"
    Then Upstash Redis reconciliation succeeds
    And the Upstash reconcile provider recorded no mutation calls

  Scenario: Pay-as-you-go databases with the same disk threshold still reconcile to fixed plans
    Given the Upstash reconcile target database has read regions "eu-west-1", coarse plan "paid", fixed plan "fixed_100gb", budget 100, and eviction disabled
    When Upstash Redis reconciliation runs with only plan "fixed_100gb"
    Then Upstash Redis reconciliation succeeds
    And the Upstash reconcile provider recorded mutation calls in order:
      | mutation |
      | plan     |
    And the Upstash reconcile target database has read regions "eu-west-1", plan "pro", budget 100, and eviction disabled

  Scenario: Deployment pipeline reconciles adopted databases
    Given the Upstash reconcile target database has read regions "eu-west-1", plan "free", budget 100, and eviction disabled
    When the Upstash Redis deployment pipeline runs for existing-only with only plan "payg"
    Then Upstash Redis reconciliation succeeds
    And the Upstash reconcile provider recorded mutation calls in order:
      | mutation |
      | plan     |
    And the Upstash reconcile target database has read regions "eu-west-1", plan "payg", budget 100, and eviction disabled
    And the Upstash Redis deployment saved remote identity database "orders-cache" with id "db-orders-cache"

  Scenario: Deployment pipeline refuses missing Redis credentials after adopt
    Given the Upstash reconcile target database has read regions "eu-west-1", plan "free", budget 100, and eviction disabled
    And the Upstash reconcile target database has no password
    When the Upstash Redis deployment pipeline runs for existing-only with only plan "payg"
    Then Upstash Redis deployment fails with provider kind "ProviderContract"
    And the Upstash Redis reconciliation failure message contains "without credentials"
    And the Upstash reconcile provider did not attempt reset-password
    And the Upstash reconcile provider recorded no mutation calls

  Scenario: Deployment pipeline refuses cached remote identity drift before adoption
    Given the Upstash reconcile target database has read regions "eu-west-1", plan "free", budget 100, and eviction disabled
    And cached Upstash remote identity for deployment is database "orders-cache" with id "db-orders-cache"
    And the Upstash reconcile target database provider name is "renamed-cache"
    And the Upstash reconcile provider has database "orders-cache" with id "db-other"
    When the Upstash Redis deployment pipeline runs for existing-only with only plan "payg"
    Then Upstash Redis deployment fails with provider kind "ProviderContract"
    And the Upstash Redis reconciliation failure message contains "Refusing to adopt a different database"
    And the Upstash reconcile provider recorded no mutation calls

  Scenario: Provider mutation failures are reported with the setting name
    Given the Upstash reconcile target database has read regions "eu-west-1", plan "free", budget 100, and eviction disabled
    And the Upstash reconcile provider fails plan mutations
    When Upstash Redis reconciliation is attempted with only plan "payg"
    Then Upstash Redis reconciliation fails for setting "plan"
    And the Upstash Redis reconciliation failure message contains "Failed to reconcile Upstash Redis database 'orders-cache' setting 'plan'"

  Scenario: Reconciliation verifies provider convergence
    Given the Upstash reconcile target database has read regions "eu-west-1", plan "free", budget 100, and eviction disabled
    And the Upstash reconcile provider does not persist budget mutations
    When Upstash Redis reconciliation is attempted with only budget 360
    Then Upstash Redis reconciliation fails for setting "budget"
    And the Upstash Redis reconciliation failure message contains "did not converge after reconciling setting 'budget'"

  Scenario: TLS is never reconciled as a mutable setting
    Given the Upstash reconcile target database has read regions "eu-west-1", plan "free", budget 100, and eviction enabled
    When Upstash Redis reconciliation runs with only TLS enabled
    Then Upstash Redis reconciliation succeeds
    And the Upstash reconcile provider recorded no mutation calls
    And the Upstash reconcile target database has read regions "eu-west-1", plan "free", budget 100, and eviction enabled

  Scenario Outline: General reconciliation exceptions default to unexpected failures
    When a general Upstash reconciliation exception is created with constructor "<Constructor>"
    Then Upstash Redis reconciliation fails with provider kind "Unexpected"

    Examples:
      | Constructor     |
      | Parameterless   |
      | Message         |
      | MessageAndInner |
