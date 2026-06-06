@live-upstash
Feature: Live Upstash provider pattern

  Scenario: Live scenarios require explicit Upstash credentials
    Given live Upstash credentials are available
    Then live Upstash cleanup is registered through the shared cleanup path
