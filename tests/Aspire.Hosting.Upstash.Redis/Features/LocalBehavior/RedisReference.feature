Feature: Redis reference behaviour

  Scenario: Marking a Redis resource for Upstash still allows normal references
    Given a standard Aspire Redis resource named "cache"
    When the Redis resource is marked for Upstash database "orders-cache"
    And a consuming container references the Redis resource
    Then the Redis reference chain is configured for the consuming container
