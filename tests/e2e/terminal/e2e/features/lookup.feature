Feature: Look up (read-only inquiry)
  As a warehouse operator who wants to know something
  I want to search stock, locations and batches without a barcode
  So that I can answer a question without starting a task

  Background:
    Given the operator opens the Look up tab

  Scenario: The full index is shown before searching
    Then "9 results" are listed

  Scenario: Searching by name narrows the results
    When the operator searches for "milk"
    Then "2 results" are listed
    And "Whole milk 3.2% — 1 L carton" is shown
    And "Greek yoghurt 400 g" is not shown

  Scenario: Filtering by entity kind
    When the operator filters by "Locations"
    Then "3 results" are listed
    And "CR1-PICKFACE-12" is shown
    And "Whole milk 3.2% — 1 L carton" is not shown

  Scenario: Blocked (QC) stock is clearly badged
    When the operator searches for "butter"
    Then a "Blocked (QC)" status badge is shown
