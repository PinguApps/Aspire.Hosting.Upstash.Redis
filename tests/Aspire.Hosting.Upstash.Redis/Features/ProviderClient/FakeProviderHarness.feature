Feature: Fake Upstash provider harness

  Scenario: Fake provider state is deterministic inside a scenario
    Given the fake Upstash provider contains database "orders-cache" in region "eu-west-1"
    When the fake Upstash provider is asked to find database "orders-cache"
    Then the fake Upstash provider returns database "orders-cache"
    And the fake Upstash provider recorded a "find-by-name" interaction for database "orders-cache"
