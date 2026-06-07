Feature: Upstash Redis ownership resolution

  Scenario: Create-only selects create when the named database is missing
    Given the Upstash ownership resolver finds no database named "orders-cache"
    When ownership is resolved for database "orders-cache" with mode "CreateOnly"
    Then the ownership resolver selects the "Create" path
    And the ownership resolver looked up database "orders-cache"

  Scenario: Create-only fails when the named database already exists
    Given the Upstash ownership resolver finds database "orders-cache" in region "eu-west-1" with TLS enabled
    When ownership is resolved for database "orders-cache" with mode "CreateOnly"
    Then ownership resolution fails because "CreateOnlyDatabaseAlreadyExists"
    And the ownership failure message contains "already exists, but ownership mode is create-only"

  Scenario: Create-only adopts the managed remote identity on repeated deploy
    Given the Upstash ownership resolver finds database "orders-cache" in region "eu-west-1" with TLS enabled
    And the existing Upstash database is the cached managed remote identity
    When ownership is resolved for database "orders-cache" with mode "CreateOnly"
    Then the ownership resolver selects the "Adopt" path
    And the ownership resolver selected database "orders-cache"

  Scenario: Existing-only adopts when the named database exists
    Given the Upstash ownership resolver finds database "orders-cache" in region "eu-west-1" with TLS enabled
    When ownership is resolved for database "orders-cache" with mode "ExistingOnly"
    Then the ownership resolver selects the "Adopt" path
    And the ownership resolver selected database "orders-cache"
    And the ownership resolver looked up database "orders-cache"

  Scenario: Existing-only fails when the named database is missing
    Given the Upstash ownership resolver finds no database named "orders-cache"
    When ownership is resolved for database "orders-cache" with mode "ExistingOnly"
    Then ownership resolution fails because "ExistingOnlyDatabaseMissing"
    And the ownership failure message contains "does not exist, but ownership mode is existing-only"

  Scenario: Create-or-adopt selects create when the named database is missing
    Given the Upstash ownership resolver finds no database named "orders-cache"
    When ownership is resolved for database "orders-cache" with mode "CreateOrAdopt"
    Then the ownership resolver selects the "Create" path
    And the ownership resolver looked up database "orders-cache"

  Scenario: Create-or-adopt adopts a compatible existing database
    Given the Upstash ownership resolver finds database "orders-cache" in region "eu-west-1" with TLS enabled
    When ownership is resolved for database "orders-cache" with mode "CreateOrAdopt"
    Then the ownership resolver selects the "Adopt" path
    And the ownership resolver selected database "orders-cache"

  Scenario: Existing database with incompatible explicit settings fails clearly
    Given the Upstash ownership resolver finds database "orders-cache" in region "us-east-1" with TLS enabled
    When ownership is resolved for database "orders-cache" with mode "CreateOrAdopt" and primary region "eu-west-1"
    Then ownership resolution fails because "ExistingDatabaseIncompatible"
    And the ownership failure message contains "immutable primary region drift"

  Scenario: Existing database with disabled TLS fails even when TLS is unset
    Given the Upstash ownership resolver finds database "orders-cache" in region "eu-west-1" with TLS disabled
    When ownership is resolved for database "orders-cache" with mode "CreateOrAdopt" and default options
    Then ownership resolution fails because "ExistingDatabaseIncompatible"
    And the ownership failure message contains "unsafe TLS drift"
