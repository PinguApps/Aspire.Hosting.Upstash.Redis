Feature: Redis reference behaviour

  Scenario: Plain Redis local usage has no Upstash deployment activity
    Given a standard Aspire Redis resource named "cache"
    And a consuming container references the Redis resource
    Then the resource remains a standard Aspire Redis resource
    And the resource keeps the standard Redis connection properties
    And the Redis reference chain is configured for the consuming container
    And the resource has no Upstash deployment metadata
    And the resource has no Upstash deployment pipeline step
    And the resource has no supplementary Upstash Redis outputs

  Scenario: Marking a Redis resource for Upstash still allows normal references
    Given a standard Aspire Redis resource named "cache"
    When the Redis resource is marked for Upstash database "orders-cache"
    And a consuming container references the Redis resource
    Then the Redis reference chain is configured for the consuming container

  Scenario: Marking a Redis resource for Upstash is a local no-op until deploy
    Given a standard Aspire Redis resource named "cache"
    When the Redis resource is marked for Upstash with literal management credentials
    And a consuming container references the Redis resource
    Then the Redis reference chain is configured for the consuming container
    And the Redis resource has no Upstash connection output
    And the Redis connection properties still use the standard Redis surface
    And the fake Upstash provider has no recorded interactions
    And the app-facing Redis outputs and references do not contain "management-secret"
