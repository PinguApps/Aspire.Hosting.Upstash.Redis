Feature: TypeScript AppHost fixture

  Scenario: Fixture consumes the generated Upstash Redis TypeScript surface
    Given the TypeScript AppHost fixture
    Then the fixture imports the generated Aspire and Upstash Redis modules
    And the fixture creates a builder and Redis resource
    And the fixture calls the generated Upstash Redis publish API with DTO options
    And the fixture consumes the generated Upstash Redis outputs
    And the fixture wires a standard Redis reference to a consuming resource
    And the fixture builds and runs the AppHost
    And the fixture keeps generated Aspire modules out of source control

  Scenario: Fixture package exposes Aspire CLI validation commands
    Given the TypeScript AppHost fixture
    Then the fixture package can restore generated SDK modules
    And the fixture package can run the TypeScript AppHost locally
    And the fixture package can wait for the local Redis resource
    And the fixture package can publish the TypeScript AppHost
    And the fixture package can type-check the generated SDK surface

  Scenario: Fixture restores and type-checks the generated TypeScript SDK
    Given the TypeScript AppHost fixture
    When the TypeScript AppHost fixture restores generated SDK modules
    Then the generated TypeScript SDK exposes the Upstash Redis surface
    And the TypeScript AppHost fixture type-checks

  Scenario: Fixture starts locally without changing normal Redis behavior
    Given the TypeScript AppHost fixture
    When the TypeScript AppHost fixture starts locally until Redis is healthy
    Then the TypeScript AppHost fixture stopped cleanly

  Scenario: Fixture publish flow builds the TypeScript AppHost
    Given the TypeScript AppHost fixture
    When the TypeScript AppHost fixture lists publish steps
    Then the TypeScript AppHost publish step listing succeeds
