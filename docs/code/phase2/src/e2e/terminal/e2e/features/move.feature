Feature: Move stock (UC-06)
  As a warehouse / forklift operator replenishing a pick face
  I want to move stock between locations with the same hard checks as put-away
  So that a move never lands goods in an incompatible room or over capacity

  Background:
    Given the operator opens the move screen

  Scenario: The move shows both legs and the compatibility checks
    Then the move leg "CR1-A03-R2-S1" is shown
    And the move leg "CR1-PICKFACE-12" is shown
    And the move product "Whole milk 3.2% — 1 L" is shown
    And the move check "Destination is a cold room (2–6 °C compatible)" passes
