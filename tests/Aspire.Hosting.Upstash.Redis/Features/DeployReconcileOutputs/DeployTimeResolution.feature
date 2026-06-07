Feature: Deploy-time Upstash parameter resolution

  Scenario: Required and optional parameter values resolve for deployment
    Given a standard Aspire Redis resource named "cache"
    When the Redis resource is marked for Upstash with resolvable parameter inputs
    And the Upstash deployment inputs are resolved
    Then the resolved Upstash deployment targets database "orders-cache"
    And the resolved Upstash management credentials use account email "owner@example.com"
    And the resolved Upstash deployment options contain the parameter values

  Scenario: Missing required parameter values fail clearly during deployment resolution
    Given a standard Aspire Redis resource named "cache"
    When the Redis resource is marked for Upstash with an unresolved API key parameter
    And resolving the Upstash deployment inputs is attempted
    Then the Upstash deployment resolution fails with "InvalidOperationException"
    And the Upstash deployment resolution failure message contains "API key parameter 'upstash-api-key'"

  Scenario: Missing pipeline context fails with argument validation
    Given a standard Aspire Redis resource named "cache"
    When the Redis resource is marked for Upstash with resolvable parameter inputs
    And executing the Upstash deployment pipeline with a missing context is attempted
    Then the Upstash deployment resolution fails with "ArgumentNullException"

  Scenario: Management API keys do not become app-facing Redis outputs
    Given a standard Aspire Redis resource named "cache"
    When the Redis resource is marked for Upstash with resolvable parameter inputs
    And the Upstash deployment inputs are resolved
    Then the resolved Upstash management API key is infrastructure-only
    And the resource keeps the standard Redis connection properties

  Scenario: Local model construction does not resolve deploy-only parameters
    Given a standard Aspire Redis resource named "cache"
    When the Redis resource is marked for Upstash with an unresolved API key parameter
    Then the resource stores parameter references for the required Upstash inputs
    And the resource keeps the standard Redis connection properties
