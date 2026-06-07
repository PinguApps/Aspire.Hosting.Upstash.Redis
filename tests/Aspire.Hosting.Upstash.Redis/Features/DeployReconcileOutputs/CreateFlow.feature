Feature: Deploy-time Upstash create flow

  Scenario: Create ownership provisions a ready database with credentials
    Given an Upstash create flow deployment for database "orders-cache"
    And ownership resolution selected create
    And the Upstash create API returns database id "db-orders"
    And the Upstash readiness API returns active database "orders-cache" with id "db-orders"
    When the Upstash create flow executes
    Then the Upstash create flow creates the database
    And the Upstash create request payload is:
      | Property       | Value        |
      | DatabaseName   | orders-cache |
      | Platform       | aws          |
      | PrimaryRegion  | eu-west-1    |
      | Plan           | payg         |
      | Budget         | 360          |
      | Eviction       | true         |
      | Tls            | true         |
    And the Upstash create request read regions are "eu-west-2"
    And the Upstash create flow returns Redis credentials for database "orders-cache"

  Scenario: Create flow waits for the created database to become ready
    Given an Upstash create flow deployment for database "orders-cache"
    And ownership resolution selected create
    And the Upstash create API returns database id "db-orders"
    And the Upstash readiness API returns active database "orders-cache" with id "db-orders"
    When the Upstash create flow executes
    Then the Upstash create flow waits for database "db-orders"

  Scenario: Create failure surfaces a clear deploy error
    Given an Upstash create flow deployment for database "orders-cache"
    And ownership resolution selected create
    And the Upstash create API fails with provider kind "Validation" and message "invalid region"
    When the Upstash create flow is attempted
    Then the Upstash create flow fails with "InvalidOperationException"
    And the Upstash create flow failure message contains "Failed to create Upstash Redis database 'orders-cache'"
    And the Upstash create flow failure message contains "invalid region"

  Scenario: Missing provider credentials after create fails as a provider contract error
    Given an Upstash create flow deployment for database "orders-cache"
    And ownership resolution selected create
    And the Upstash create API returns database id "db-orders"
    And the Upstash readiness API returns active database "orders-cache" with id "db-orders" without a password
    When the Upstash create flow is attempted
    Then the Upstash create flow fails with "UpstashRedisProviderException"
    And the Upstash create flow fails with provider kind "ProviderContract"
    And the Upstash create flow failure message contains "without credentials"

  Scenario: Adopt ownership skips database creation
    Given an Upstash create flow deployment for database "orders-cache"
    And ownership resolution selected adopt for database "orders-cache" with id "db-orders"
    When the Upstash create flow executes
    Then the Upstash create flow does not create the database
    And the Upstash create flow returns Redis credentials for database "orders-cache"
