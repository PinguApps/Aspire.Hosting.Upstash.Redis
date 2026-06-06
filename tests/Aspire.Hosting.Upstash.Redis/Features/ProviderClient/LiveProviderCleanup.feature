Feature: Live Upstash provider cleanup

  Scenario: Cleanup continues when a registered action fails
    Given live Upstash cleanup has an older action registered
    And live Upstash cleanup has a newer failing action registered
    When live Upstash cleanup runs
    Then the older live Upstash cleanup action ran
    And the live Upstash cleanup failure is reported
