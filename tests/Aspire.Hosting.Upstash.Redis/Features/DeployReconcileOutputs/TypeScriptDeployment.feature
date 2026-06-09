Feature: TypeScript-authored Upstash deployment

  Scenario: TypeScript bridge deployment reuses the managed identity on repeat deploy
    Given a TypeScript-authored Upstash Redis deployment for database "orders-cache"
    And the TypeScript deployment fake provider has no database named "orders-cache"
    When the TypeScript-authored Upstash deployment pipeline runs twice
    Then the TypeScript-authored Upstash deployment created 1 database
    And the TypeScript-authored Upstash deployments returned the same provider id
    And the TypeScript-authored Upstash deployment populated Redis outputs for database "orders-cache"

  @live-upstash
  Scenario: Live TypeScript bridge deployment repeats against one disposable database
    Given a live TypeScript-authored Upstash Redis deployment with prefix "pin-183-ts"
    When the live TypeScript-authored Upstash deployment pipeline runs twice
    Then the live TypeScript-authored Upstash deployments returned the same provider id
    And only one live TypeScript-authored Upstash database exists with the configured name
    And the live TypeScript-authored Upstash database is registered for deletion
