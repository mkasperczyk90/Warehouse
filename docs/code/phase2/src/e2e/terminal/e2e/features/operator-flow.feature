Feature: Completing tasks and the outbound chain
  As a warehouse operator
  I want each confirmed task to leave its pile and hand off to the next step
  So that the hub always reflects what is left to do, end to end

  Background:
    Given the operator opens the terminal

  Scenario: Completing a put-away drops its pile
    When the operator taps the "Put away" pile
    And the operator confirms the put-away
    Then the task hub is shown again
    And the "Put away" pile shows "13"

  Scenario: Completing a move drops its pile
    When the operator taps the "Move stock" pile
    And the operator confirms the move
    Then the task hub is shown again
    And the "Move stock" pile shows "4"

  Scenario: An inter-warehouse transfer also clears the move task
    When the operator taps the "Move stock" pile
    And the operator issues an inter-warehouse transfer
    Then the task hub is shown again
    And the "Move stock" pile shows "4"

  Scenario: The operator walks the outbound chain pick → pack → hub
    When the operator taps the "Pick" pile
    And the operator scans the location and the product
    And the operator confirms the pick
    Then the packing screen opens
    When the operator closes the package
    Then the task hub is shown again
