Feature: Switching the active warehouse
  As a warehouse desk manager
  I want to switch which warehouse I'm viewing
  So that every screen shows that site's stock and work

  Background:
    Given the manager opens the Stock view

  Scenario: Switching warehouse re-scopes the stock table
    Then the stock row "Cheese wheel 5 kg" is shown
    When the manager switches to warehouse "WH-02 Poznań"
    Then the active warehouse is "Poznań"
    And the stock row "Frozen peas 1 kg" is shown
    And the stock row "Cheese wheel 5 kg" is not shown
