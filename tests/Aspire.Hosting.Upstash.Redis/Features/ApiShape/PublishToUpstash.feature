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
