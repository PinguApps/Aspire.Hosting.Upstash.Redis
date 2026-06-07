Feature: Upstash Redis management client

  Scenario: Auth headers are constructed for management requests
    Given the Upstash management API returns an empty database list
    When the Upstash management client lists databases with account "pingu@example.com" and API key "secret-key"
    Then the Upstash management request uses GET "/v2/redis/databases"
    And the Upstash management request has the expected Basic auth header for account "pingu@example.com" and API key "secret-key"

  Scenario: Database details are parsed from credential-bearing responses
    Given the Upstash management API returns database details for "orders-cache"
    When the Upstash management client gets database "db-orders"
    Then the Upstash management client returns database "orders-cache" with credentials

  Scenario: Database lookup by name uses list then detail fetch
    Given the Upstash management API returns a list containing database "orders-cache"
    And the Upstash management API returns database details for "orders-cache"
    When the Upstash management client finds database "orders-cache" by name
    Then the Upstash management client returns database "orders-cache" with credentials
    And the Upstash management request sequence is:
      | Method | Path                        |
      | GET    | /v2/redis/databases         |
      | GET    | /v2/redis/database/db-orders |

  Scenario: Duplicate database names are surfaced as a provider contract failure
    Given the Upstash management API returns duplicate databases named "orders-cache"
    When the Upstash management client finds database "orders-cache" by name
    Then the Upstash management client fails with provider kind "ProviderContract"

  Scenario: Detail lookup id drift is surfaced as a provider contract failure
    Given the Upstash management API returns a list containing database "orders-cache"
    And the Upstash management API returns database details for "orders-cache" with id "db-other"
    When the Upstash management client finds database "orders-cache" by name
    Then the Upstash management client fails with provider kind "ProviderContract"

  Scenario: Database creation sends the supported request body
    Given the Upstash management API returns database details for "orders-cache"
    When the Upstash management client creates database "orders-cache"
    Then the Upstash management request uses POST "/v2/redis/database"
    And the Upstash management request body contains:
      | Property       | Value        |
      | database_name  | orders-cache |
      | platform       | aws          |
      | primary_region | eu-west-1    |
      | plan           | payg         |
      | budget         | 50           |
      | eviction       | true         |
      | tls            | true         |

  Scenario: Mutable operations use the supported provider endpoints
    Given the Upstash management API returns OK for five operations
    When the Upstash management client updates mutable settings for database "db-orders"
    Then the Upstash management request sequence is:
      | Method | Path                                  |
      | POST   | /v2/redis/update-regions/db-orders    |
      | POST   | /v2/redis/db-orders/change-plan       |
      | PATCH  | /v2/redis/update-budget/db-orders     |
      | POST   | /v2/redis/enable-eviction/db-orders   |
      | POST   | /v2/redis/disable-eviction/db-orders  |

  Scenario: Readiness polling returns when the database is active
    Given the Upstash management API returns a modifying database then an active database
    When the Upstash management client waits for database "db-orders" to become ready
    Then the Upstash management client returns database "orders-cache" with credentials

  Scenario: Missing password is surfaced as a provider contract failure
    Given the Upstash management API returns database details without a password
    When the Upstash management client gets database "db-orders"
    Then the Upstash management client fails with provider kind "ProviderContract"
    And the Upstash management client did not request reset-password

  Scenario: Provider validation errors are classified and sanitized
    Given the Upstash management API returns status 400 with error "invalid secret-key setting"
    When the Upstash management client lists databases with account "pingu@example.com" and API key "secret-key"
    Then the Upstash management client fails with provider kind "Validation"
    And the Upstash management failure message does not contain "secret-key"

  Scenario: Provider auth failures are classified predictably
    Given the Upstash management API returns status 401 with error "Unauthorized"
    When the Upstash management client lists databases with account "pingu@example.com" and API key "secret-key"
    Then the Upstash management client fails with provider kind "Authentication"

  Scenario Outline: Transport failures are classified as transient
    Given the Upstash management API fails before responding with "<Failure>"
    When the Upstash management client lists databases with account "pingu@example.com" and API key "secret-key"
    Then the Upstash management client fails with provider kind "Transient"

    Examples:
      | Failure          |
      | RequestException |
      | Timeout          |

  Scenario Outline: General provider exceptions default to unexpected failures
    When a general Upstash provider exception is created with constructor "<Constructor>"
    Then the Upstash management client fails with provider kind "Unexpected"

    Examples:
      | Constructor     |
      | Parameterless   |
      | Message         |
      | MessageAndInner |

  Scenario: Request cancellation is preserved
    Given the Upstash management API waits until cancellation
    When the Upstash management client lists databases with a cancelled token
    Then the Upstash management client operation is cancelled
