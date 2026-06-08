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
    And the fixture package can publish the TypeScript AppHost
    And the fixture package can type-check the generated SDK surface
