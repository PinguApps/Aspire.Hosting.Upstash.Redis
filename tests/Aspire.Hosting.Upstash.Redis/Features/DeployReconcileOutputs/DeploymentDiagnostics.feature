Feature: Upstash Redis deployment diagnostics

  Scenario: Deployment progress reports create phases in order
    Given an Upstash diagnostic deployment for database "orders-cache"
    And the Upstash diagnostic provider has no existing database
    When the Upstash diagnostic deployment pipeline runs
    Then the Upstash diagnostic progress phases are:
      | phase                     |
      | ResolvingConfiguration    |
      | LocatingDatabase          |
      | LocatingDatabase          |
      | ValidatingImmutableDrift  |
      | CreatingDatabase          |
      | CreatingDatabase          |
      | ReconcilingMutableSettings |
      | RetrievingOutputs         |

  Scenario: Deployment progress reports adopt phases with provider identifiers
    Given an Upstash diagnostic deployment for database "orders-cache"
    And the Upstash diagnostic provider has existing database "orders-cache" with id "db-orders"
    When the Upstash diagnostic deployment pipeline runs
    Then the Upstash diagnostic progress contains "Located Upstash Redis database 'orders-cache' with provider id 'db-orders'"
    And the Upstash diagnostic progress contains provider id "db-orders"

  Scenario: Deployment diagnostics redact secrets
    Given an Upstash diagnostic deployment for database "orders-cache"
    And the Upstash diagnostic provider has existing database "orders-cache" with id "db-orders"
    When the Upstash diagnostic message "api-key-secret redis://default:redis-password@global-apt-1.upstash.io:6379" is redacted
    Then the redacted Upstash diagnostic message does not contain "api-key-secret"
    And the redacted Upstash diagnostic message does not contain "redis-password"
    And the redacted Upstash diagnostic message does not contain "redis://default:redis-password@global-apt-1.upstash.io:6379"
    And the redacted Upstash diagnostic message contains "[redacted]"

  Scenario: Deployment progress keeps failure context actionable
    Given an Upstash diagnostic deployment for database "orders-cache"
    And the Upstash diagnostic provider has existing database "orders-cache" with id "db-orders"
    And the Upstash diagnostic provider fails plan mutations
    When the Upstash diagnostic deployment pipeline is attempted
    Then the Upstash diagnostic deployment failure message contains "Failed to reconcile Upstash Redis database 'orders-cache' setting 'plan'"
    And the Upstash diagnostic progress contains "Reconciling explicit mutable Upstash Redis settings for database 'orders-cache'"
