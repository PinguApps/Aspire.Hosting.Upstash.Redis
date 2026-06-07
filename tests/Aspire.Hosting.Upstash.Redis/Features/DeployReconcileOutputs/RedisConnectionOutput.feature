Feature: Redis connection output

  Scenario: Deployed Upstash Redis output resolves through the normal Redis reference
    Given a standard Aspire Redis resource named "cache"
    And a consuming container references the Redis resource
    When Upstash Redis connection output is applied with endpoint "global-apt-1.upstash.io", port 6379, password "redis-password", and TLS enabled
    Then the Redis connection string reference resolves to "global-apt-1.upstash.io:6379,password=redis-password,ssl=true"
    And the Redis connection properties contain:
      | Name     | Value                                               |
      | Host     | global-apt-1.upstash.io                            |
      | Port     | 6379                                                |
      | Password | redis-password                                      |
      | Uri      | rediss://:redis-password@global-apt-1.upstash.io:6379 |
    And the Redis connection output does not contain "management-secret"

  Scenario: Upstash publishing does not redirect local Redis connection output before deploy
    Given a standard Aspire Redis resource named "cache"
    When the Redis resource is marked for Upstash database "orders-cache"
    Then the Redis resource has no Upstash connection output
    And the Redis connection properties still use the standard Redis surface

  Scenario: Provider endpoint slugs are rejected for Redis connection output
    Given a standard Aspire Redis resource named "cache"
    When applying Upstash Redis connection output with endpoint "global-apt-1" is attempted
    Then Upstash Redis connection output fails with provider kind "ProviderContract"
    And the Upstash Redis connection output failure message contains "complete host name"
    And the Redis resource has no Upstash connection output

  Scenario: Missing provider endpoints are rejected for Redis connection output
    Given a standard Aspire Redis resource named "cache"
    When applying Upstash Redis connection output without an endpoint is attempted
    Then Upstash Redis connection output fails with provider kind "ProviderContract"
    And the Upstash Redis connection output failure message contains "without an endpoint"
    And the Redis resource has no Upstash connection output

  Scenario: Missing provider passwords are rejected for Redis connection output
    Given a standard Aspire Redis resource named "cache"
    When applying Upstash Redis connection output without a password is attempted
    Then Upstash Redis connection output fails with provider kind "ProviderContract"
    And the Upstash Redis connection output failure message contains "without credentials"
    And the Redis resource has no Upstash connection output
