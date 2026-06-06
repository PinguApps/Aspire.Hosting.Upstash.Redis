Feature: Publish Redis to Upstash

  Scenario: Marking a Redis resource for Upstash keeps the standard Redis resource model
    Given a standard Aspire Redis resource named "cache"
    When the Redis resource is marked for Upstash database "orders-cache"
    Then the resource remains a standard Aspire Redis resource
    And the resource has Upstash deployment metadata for database "orders-cache"
    And the resource keeps the standard Redis connection properties

  Scenario: Marking a Redis resource for Upstash still allows normal references
    Given a standard Aspire Redis resource named "cache"
    When the Redis resource is marked for Upstash database "orders-cache"
    And a consuming container references the Redis resource
    Then the Redis reference chain is configured for the consuming container
