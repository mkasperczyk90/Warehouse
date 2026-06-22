Feature: Picking (UC-10)
  As a warehouse operator working a released wave
  I want a routed pick with the FEFO batch shown and the scan as the commit
  So that I pick the right batch and never confirm from memory

  Background:
    Given the operator opens the picking screen

  Scenario: The pick shows the go-to location, product and FEFO batch
    Then the go-to location "WH01-A2-A07-R3-S2" is shown
    And the product to pick is "Greek yoghurt 400 g"
    And the FEFO batch "FEFO · BBE 2026-06-28 · LOT-0419" is shown

  Scenario: The pick cannot be confirmed until both scans land
    Then the pick cannot be confirmed yet
    When the operator scans the location and the product
    Then the pick can be confirmed

  Scenario: Confirming the pick advances to packing
    When the operator scans the location and the product
    And the operator confirms the pick
    Then the packing screen opens

  Scenario: A short pick replans onto the next FEFO batch
    Then the FEFO batch "FEFO · BBE 2026-06-28 · LOT-0419" is shown
    When the operator reports a short pick "Less stock at this location"
    Then the go-to location "WH01-A2-A09-R1-S2" is shown
    And the FEFO batch "FEFO · BBE 2026-07-05 · LOT-0511" is shown
