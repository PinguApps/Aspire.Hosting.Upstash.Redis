Feature: Publish Redis to Upstash

  Scenario: Marking a Redis resource for Upstash keeps the standard Redis resource model
    Given a standard Aspire Redis resource named "cache"
    When the Redis resource is marked for Upstash database "orders-cache"
    Then the resource remains a standard Aspire Redis resource
    And the resource has Upstash deployment metadata for database "orders-cache"
    And mutating captured callback options cannot mutate deployment metadata
    And the explicit setting snapshot cannot mutate deployment metadata
    And mutating the configured read regions cannot mutate deployment metadata
    And the resource keeps the standard Redis connection properties

  Scenario: Reconfiguring a Redis resource for Upstash replaces deployment intent
    Given a standard Aspire Redis resource named "cache"
    When the Redis resource is marked for Upstash database "orders-cache"
    And the Redis resource is marked for Upstash database "updated-orders-cache"
    Then the resource has Upstash deployment metadata for database "updated-orders-cache"
    And the resource has one Upstash deployment pipeline step

  Scenario Outline: Marking a Redis resource for Upstash captures the requested ownership mode
    Given a standard Aspire Redis resource named "cache"
    When the Redis resource is marked for Upstash database "orders-cache" with ownership mode "<ownershipMode>"
    Then the resource has Upstash ownership mode "<ownershipMode>"

    Examples:
      | ownershipMode  |
      | CreateOnly     |
      | ExistingOnly   |
      | CreateOrAdopt  |

  Scenario Outline: PublishToUpstash overloads capture equivalent deployment intent
    Given a standard Aspire Redis resource named "cache"
    When the Redis resource is marked for Upstash through the "<overload>" overload
    Then the resource has Upstash ownership mode "ExistingOnly"
    And the Upstash deployment metadata matches the "<overload>" overload
    And the fluent API returns the same Redis resource builder

    Examples:
      | overload                                   |
      | literal database and parameter credentials |
      | parameter database and parameter credentials |
      | literal deployment values                  |

  Scenario: Marking a Redis resource for Upstash supports parameter-based inputs
    Given a standard Aspire Redis resource named "cache"
    When the Redis resource is marked for Upstash with parameter-based inputs
    Then the resource stores parameter references for the required Upstash inputs
    And the resource stores parameter references for optional Upstash inputs
    And the provider domain preserves parameter-backed option sources

  Scenario: Marking a Redis resource for Upstash maps typed domain values to provider payload values
    Given a standard Aspire Redis resource named "cache"
    When the Redis resource is marked for Upstash with typed domain options
    Then the provider domain maps the typed options to Upstash payload values
    And the provider domain preserves explicit settings for reconcile

  Scenario: Marking a Redis resource for Upstash preserves explicit option intent
    Given a standard Aspire Redis resource named "cache"
    When the Redis resource is marked for Upstash with an explicitly unset primary region
    Then the Upstash state distinguishes the explicitly unset primary region from an unspecified plan

  Scenario: Marking a Redis resource for Upstash rejects a blank database name
    Given a standard Aspire Redis resource named "cache"
    When the Redis resource is marked for a blank Upstash database name
    Then the Upstash configuration fails with "ArgumentException"

  Scenario: Marking a Redis resource for Upstash rejects a missing API key value
    Given a standard Aspire Redis resource named "cache"
    When the Redis resource is marked for Upstash with a missing API key value
    Then the Upstash configuration fails with "ArgumentNullException"

  Scenario: Marking a Redis resource for Upstash rejects an unsupported ownership mode
    Given a standard Aspire Redis resource named "cache"
    When the Redis resource is marked for Upstash with an unsupported ownership mode
    Then the Upstash configuration fails with "ArgumentOutOfRangeException"

  Scenario: Marking a Redis resource for Upstash rejects disabled TLS
    Given a standard Aspire Redis resource named "cache"
    When the Redis resource is marked for Upstash with disabled TLS
    Then the Upstash configuration fails with "InvalidOperationException"
    And the Upstash configuration failure message contains "requires TLS"

  Scenario: Marking a Redis resource for Upstash rejects an unsupported platform
    Given a standard Aspire Redis resource named "cache"
    When the Redis resource is marked for Upstash with unsupported platform
    Then the Upstash configuration fails with "InvalidOperationException"
    And the Upstash configuration failure message contains "platform 'azure' is not supported"

  Scenario: Marking a Redis resource for Upstash rejects mismatched platform and primary region
    Given a standard Aspire Redis resource named "cache"
    When the Redis resource is marked for Upstash with mismatched platform and primary region
    Then the Upstash configuration fails with "InvalidOperationException"
    And the Upstash configuration failure message contains "primary region 'us-central1' is a gcp region and cannot be used with platform 'aws'"

  Scenario: Marking a Redis resource for Upstash rejects budget on fixed plans
    Given a standard Aspire Redis resource named "cache"
    When the Redis resource is marked for Upstash with a fixed plan budget
    Then the Upstash configuration fails with "InvalidOperationException"
    And the Upstash configuration failure message contains "budget can only be configured with the pay-as-you-go plan"
