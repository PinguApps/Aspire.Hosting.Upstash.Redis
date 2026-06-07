Feature: Supplementary Upstash Redis outputs

  Scenario: Deployment populates supplementary outputs from provider details
    Given an Upstash Redis resource with supplementary outputs
    And the Upstash deployment provider will create database "orders-cache" with id "db-orders"
    When the Upstash deployment pipeline populates supplementary outputs
    Then the supplementary Upstash Redis outputs are:
      | Name         | Value                    |
      | Endpoint     | global-apt-1.upstash.io  |
      | Port         | 6379                     |
      | Password     | redis-password           |
      | Tls          | true                     |
      | DatabaseName | orders-cache             |
    And only the supplementary Upstash Redis password output is secret
    And the Upstash management API key is not surfaced as a supplementary output
    And the supplementary Upstash Redis output names are stable
    And each supplementary Upstash Redis output references the Redis resource
