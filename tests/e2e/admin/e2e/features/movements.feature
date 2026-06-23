Feature: Stock movements ledger
  As a warehouse manager
  I want to read the immutable movement ledger
  So that I can trace how stock came to be — stock is a projection of these entries

  Background:
    Given the manager opens the movements ledger

  Scenario: The ledger lists movements
    Then the movements ledger is shown
    And the movement reference "GR-2206" is shown
    And the movement reference "SO-4470" is shown

  Scenario: Filtering the ledger by movement type
    When the manager filters movements by type "Pick"
    Then the movement reference "SO-4470" is shown
    And the movement reference "GR-2206" is no longer in the ledger

  Scenario: Filtering the ledger by search text
    When the manager searches movements for "FZ1"
    Then the movement reference "PA-1176" is shown
    And the movement reference "GR-2206" is no longer in the ledger
