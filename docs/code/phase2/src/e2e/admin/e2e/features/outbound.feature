Feature: Outbound orders (UC-09)
  As a logistics coordinator
  I want to create orders and decide split/hold, release or cancel
  So that customer demand is reserved against ATP and moved through its lifecycle

  Background:
    Given the coordinator opens the outbound orders

  Scenario: Orders and the first order's lines are shown, with the partial rule
    Then the outbound list is shown
    And "SO-4471" is shown
    And "Greek yoghurt 400 g" is shown
    And "partial / waiting order" is shown

  Scenario: Creating an order needs a customer and at least one line (UC-09 step 1)
    When the coordinator starts a new order
    And the coordinator enters the customer "New Bistro"
    Then the create-order button is disabled
    When the coordinator adds an order line with SKU "5900000000002" and quantity 20
    Then the create-order button is enabled
    When the coordinator submits the order
    Then "— New Bistro" is shown

  Scenario: Splitting a partially-reserved order (UC-09 coordinator decision)
    When the coordinator selects order "SO-4472"
    Then "SO-4472 — Bistro 24" is shown
    When the coordinator splits the order
    Then "available portion reserved" is shown

  Scenario: Releasing a reserved order to a picking wave
    When the coordinator selects order "SO-4471"
    Then "SO-4471 — Fresh Market sp. z o.o." is shown
    When the coordinator releases the order to a wave
    Then "Released to wave" is shown

  Scenario: Cancelling an order releases its reservations
    When the coordinator selects order "SO-4469"
    Then "SO-4469 — Fresh Market sp. z o.o." is shown
    When the coordinator cancels the order
    Then "reservations released back to ATP" is shown
