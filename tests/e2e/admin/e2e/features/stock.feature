Feature: Stock view (inventory inquiry)
  As a warehouse desk manager
  I want to search and filter on-hand stock
  So that I can answer "where is X / how much do we have" and drill into an item

  Background:
    Given the manager opens the Stock view

  Scenario: The KPIs and the full stock table are shown
    Then the Stock view is shown
    And the stock row "Whole milk 3.2% 1 L" is shown
    And the stock row "Greek yoghurt 400 g" is shown

  Scenario: Searching by name narrows the table
    When the manager searches stock for "milk"
    Then the stock row "Whole milk 3.2% 1 L" is shown
    And the stock row "Greek yoghurt 400 g" is not shown

  Scenario: Filtering by a quick-filter pill
    When the manager filters stock by "Blocked"
    Then the stock row "Cheese wheel 5 kg" is shown
    And the stock row "Whole milk 3.2% 1 L" is not shown

  Scenario: Opening a row drills into the stock item
    When the manager opens the stock row "Whole milk 3.2% 1 L"
    Then the URL is "/stock/1"
    And "Whole milk 3.2% 1 L · LOT-0425-A" is shown
