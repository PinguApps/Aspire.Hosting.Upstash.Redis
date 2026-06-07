Feature: Upstash Redis ownership deployment

  Scenario: Create-only creates a missing database through the deployment pipeline
    Given an Upstash ownership deployment for database "orders-cache" with mode "CreateOnly"
    And the Upstash ownership deployment provider has no database named "orders-cache"
    When the Upstash ownership deployment pipeline runs
    Then the Upstash ownership deployment succeeds using the "Create" path
    And the Upstash ownership deployment saved remote identity database "orders-cache"
    And the Upstash ownership deployment populated Redis outputs for database "orders-cache"

  Scenario: Create-only fails when an unmanaged database already exists
    Given an Upstash ownership deployment for database "orders-cache" with mode "CreateOnly"
    And the Upstash ownership deployment provider has database "orders-cache" with id "db-orders"
    When the Upstash ownership deployment pipeline is attempted
    Then the Upstash ownership deployment fails because "CreateOnlyDatabaseAlreadyExists"
    And the Upstash ownership deployment failure message contains "already exists, but ownership mode is create-only"
    And the Upstash ownership deployment did not create a database

  Scenario: Existing-only adopts an existing database through the deployment pipeline
    Given an Upstash ownership deployment for database "orders-cache" with mode "ExistingOnly"
    And the Upstash ownership deployment provider has database "orders-cache" with id "db-orders"
    When the Upstash ownership deployment pipeline runs
    Then the Upstash ownership deployment succeeds using the "Adopt" path
    And the Upstash ownership deployment saved remote identity database "orders-cache"
    And the Upstash ownership deployment populated Redis outputs for database "orders-cache"

  Scenario: Existing-only fails when the named database is missing
    Given an Upstash ownership deployment for database "orders-cache" with mode "ExistingOnly"
    And the Upstash ownership deployment provider has no database named "orders-cache"
    When the Upstash ownership deployment pipeline is attempted
    Then the Upstash ownership deployment fails because "ExistingOnlyDatabaseMissing"
    And the Upstash ownership deployment failure message contains "does not exist, but ownership mode is existing-only"
    And the Upstash ownership deployment did not create a database

  Scenario: Create-or-adopt creates a missing database through the deployment pipeline
    Given an Upstash ownership deployment for database "orders-cache" with mode "CreateOrAdopt"
    And the Upstash ownership deployment provider has no database named "orders-cache"
    When the Upstash ownership deployment pipeline runs
    Then the Upstash ownership deployment succeeds using the "Create" path
    And the Upstash ownership deployment saved remote identity database "orders-cache"
    And the Upstash ownership deployment populated Redis outputs for database "orders-cache"

  Scenario: Create-or-adopt adopts an existing database through the deployment pipeline
    Given an Upstash ownership deployment for database "orders-cache" with mode "CreateOrAdopt"
    And the Upstash ownership deployment provider has database "orders-cache" with id "db-orders"
    When the Upstash ownership deployment pipeline runs
    Then the Upstash ownership deployment succeeds using the "Adopt" path
    And the Upstash ownership deployment saved remote identity database "orders-cache"
    And the Upstash ownership deployment populated Redis outputs for database "orders-cache"

  Scenario Outline: Repeated deployments reuse the managed identity without recreating
    Given an Upstash ownership deployment for database "orders-cache" with mode "<Mode>"
    And the Upstash ownership deployment provider has no database named "orders-cache"
    When the Upstash ownership deployment pipeline runs
    And the Upstash ownership deployment pipeline runs again
    Then the Upstash ownership deployment created 1 database
    And the Upstash ownership deployment succeeded using the "Adopt" path
    And the Upstash ownership deployment saved remote identity database "orders-cache"

    Examples:
      | Mode          |
      | CreateOnly    |
      | CreateOrAdopt |

  Scenario: Repeated existing-only deployments keep adopting the same managed identity
    Given an Upstash ownership deployment for database "orders-cache" with mode "ExistingOnly"
    And the Upstash ownership deployment provider has database "orders-cache" with id "db-orders"
    When the Upstash ownership deployment pipeline runs
    And the Upstash ownership deployment pipeline runs again
    Then the Upstash ownership deployment created 0 databases
    And the Upstash ownership deployment succeeded using the "Adopt" path
    And the Upstash ownership deployment saved remote identity database "orders-cache"

  Scenario: Duplicate configured database names fail before ownership adoption
    Given an Upstash ownership deployment for database "orders-cache" with mode "CreateOrAdopt"
    And the Upstash ownership deployment provider has duplicate databases named "orders-cache"
    When the Upstash ownership deployment pipeline is attempted
    Then the Upstash ownership deployment fails with provider kind "ProviderContract"
    And the Upstash ownership deployment failure message contains "more than one database named 'orders-cache'"
    And the Upstash ownership deployment did not create a database

  Scenario: Cached identity refuses to adopt a different database for the same configured name
    Given an Upstash ownership deployment for database "orders-cache" with mode "ExistingOnly"
    And cached Upstash ownership deployment identity is database "orders-cache" with id "db-orders"
    And the Upstash ownership deployment provider has database "renamed-cache" with id "db-orders"
    And the Upstash ownership deployment provider has database "orders-cache" with id "db-other"
    When the Upstash ownership deployment pipeline is attempted
    Then the Upstash ownership deployment fails with provider kind "ProviderContract"
    And the Upstash ownership deployment failure message contains "Refusing to adopt a different database"
    And the Upstash ownership deployment did not create a database

  @live-upstash
  Scenario: Live create-or-adopt creates an isolated database and registers deletion cleanup
    Given a live Upstash ownership deployment for isolated database prefix "pin-170-create"
    When the live Upstash ownership deployment runs with mode "CreateOrAdopt"
    Then the live Upstash ownership deployment created a database
    And the live Upstash ownership deployment registered delete cleanup

  @live-upstash
  Scenario: Live existing-only adopts an isolated database and registers deletion cleanup
    Given a live Upstash ownership deployment for isolated database prefix "pin-170-adopt"
    And the live Upstash ownership provider has an isolated database to adopt
    When the live Upstash ownership deployment runs with mode "ExistingOnly"
    Then the live Upstash ownership deployment adopted the database
    And the live Upstash ownership deployment registered delete cleanup
