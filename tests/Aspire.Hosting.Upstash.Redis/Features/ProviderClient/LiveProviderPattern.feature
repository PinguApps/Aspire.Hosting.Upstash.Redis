Feature: Live Upstash provider pattern

  @live-upstash
  Scenario: Live scenarios require explicit Upstash credentials
    Given live Upstash credentials are available
    Then live Upstash cleanup is registered through the shared cleanup path

  Scenario: Live cleanup rejects null cleanup actions
    When a null live Upstash cleanup action is registered
    Then live Upstash cleanup registration fails for a null cleanup action

  Scenario: Live cleanup runs every registered action before reporting failures
    Given live Upstash cleanup action "first" fails
    And live Upstash cleanup action "second" succeeds
    And live Upstash cleanup action "third" fails
    When live Upstash cleanup runs
    Then every live Upstash cleanup action has run
    And live Upstash cleanup reports 2 failures

  Scenario: Live disposable database names keep a suffix for long prefixes
    When live disposable database names are generated with prefix "pin-171-feedback-prefix-with-more-than-thirty-one-chars"
    Then each live disposable database name is at most 40 characters
    And each live disposable database name ends with an 8 character GUID suffix
    And the live disposable database names are unique
