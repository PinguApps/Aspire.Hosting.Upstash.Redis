Feature: Upstash Redis remote identity

  Scenario: First deployment finds an existing database by configured name
    Given the Upstash identity API returns a list containing database "orders-cache" with id "db-orders"
    And the Upstash identity API returns details for database "orders-cache" with id "db-orders"
    When the Upstash remote identity resolver resolves configured database "orders-cache"
    Then the Upstash remote identity resolver returns database "orders-cache" with id "db-orders"
    And the Upstash remote identity was not resolved from the cached identity
    And the Upstash remote identity cache is database "orders-cache" with id "db-orders"
    And the Upstash identity request sequence is:
      | Method | Path                        |
      | GET    | /v2/redis/databases         |
      | GET    | /v2/redis/database/db-orders |

  Scenario: First deployment reports no existing database when the configured name is absent
    Given the Upstash identity API returns an empty database list
    When the Upstash remote identity resolver resolves configured database "orders-cache"
    Then the Upstash remote identity resolver returns no database
    And the Upstash remote identity cache is empty
    And the Upstash identity request sequence is:
      | Method | Path                |
      | GET    | /v2/redis/databases |

  Scenario: Repeated deployment reuses the cached provider id when the name still matches
    Given cached Upstash remote identity is database "orders-cache" with id "db-orders"
    And the Upstash identity API returns details for database "orders-cache" with id "db-orders"
    And the Upstash identity API returns a list containing database "orders-cache" with id "db-orders"
    And the Upstash identity API returns details for database "orders-cache" with id "db-orders"
    When the Upstash remote identity resolver resolves configured database "orders-cache"
    Then the Upstash remote identity resolver returns database "orders-cache" with id "db-orders"
    And the Upstash remote identity was resolved from the cached identity
    And the Upstash remote identity cache is database "orders-cache" with id "db-orders"
    And the Upstash identity request sequence is:
      | Method | Path                        |
      | GET    | /v2/redis/database/db-orders |
      | GET    | /v2/redis/databases         |
      | GET    | /v2/redis/database/db-orders |

  Scenario: Cached identity still checks duplicate configured names before reuse
    Given cached Upstash remote identity is database "orders-cache" with id "db-orders"
    And the Upstash identity API returns details for database "orders-cache" with id "db-orders"
    And the Upstash identity API returns duplicate databases named "orders-cache"
    When the Upstash remote identity resolver resolves configured database "orders-cache"
    Then the Upstash remote identity resolver fails with provider kind "ProviderContract"

  Scenario: Cached identity refuses a detail response with a different provider id
    Given cached Upstash remote identity is database "orders-cache" with id "db-orders"
    And the Upstash identity API returns details for database "orders-cache" with id "db-other"
    When the Upstash remote identity resolver resolves configured database "orders-cache"
    Then the Upstash remote identity resolver fails with provider kind "ProviderContract"
    And the Upstash remote identity failure message contains "mismatched cached remote identity"

  Scenario: Explicit configured name changes select the new configured remote identity
    Given cached Upstash remote identity is database "orders-cache" with id "db-orders"
    And the Upstash identity API returns a list containing database "billing-cache" with id "db-billing"
    And the Upstash identity API returns details for database "billing-cache" with id "db-billing"
    When the Upstash remote identity resolver resolves configured database "billing-cache"
    Then the Upstash remote identity resolver returns database "billing-cache" with id "db-billing"
    And the Upstash remote identity cache is database "billing-cache" with id "db-billing"
    And the Upstash identity request sequence is:
      | Method | Path                         |
      | GET    | /v2/redis/databases          |
      | GET    | /v2/redis/database/db-billing |

  Scenario: Duplicate configured names fail clearly
    Given the Upstash identity API returns duplicate databases named "orders-cache"
    When the Upstash remote identity resolver resolves configured database "orders-cache"
    Then the Upstash remote identity resolver fails with provider kind "ProviderContract"

  Scenario: Detail lookup name drift fails clearly
    Given the Upstash identity API returns a list containing database "orders-cache" with id "db-orders"
    And the Upstash identity API returns details for database "renamed-cache" with id "db-orders"
    When the Upstash remote identity resolver resolves configured database "orders-cache"
    Then the Upstash remote identity resolver fails with provider kind "ProviderContract"

  Scenario: Cached identity refuses to take over a different provider id for the same configured name
    Given cached Upstash remote identity is database "orders-cache" with id "db-orders"
    And the Upstash identity API returns details for database "renamed-cache" with id "db-orders"
    And the Upstash identity API returns a list containing database "orders-cache" with id "db-other"
    And the Upstash identity API returns details for database "orders-cache" with id "db-other"
    When the Upstash remote identity resolver resolves configured database "orders-cache"
    Then the Upstash remote identity resolver fails with provider kind "ProviderContract"
    And the Upstash remote identity failure message contains "Refusing to adopt a different database"

  Scenario: Remote identity state can be persisted in Aspire deployment state
    When the Upstash remote identity cache for Redis resource "cache" is saved as database "orders-cache" with id "db-orders"
    Then the Upstash remote identity cache for Redis resource "cache" loads database "orders-cache" with id "db-orders"

  Scenario: Missing remote identity state loads as empty cache
    Then the Upstash remote identity cache for Redis resource "cache" is empty
