Feature: Today worklist (the desk's landing)
  As a warehouse desk manager
  I want my work queues the moment I open the panel
  So that I can clear what needs me now, not read a dashboard

  Background:
    Given the manager opens the admin panel

  Scenario: The worklist greets the manager with actionable cards
    Then the Today worklist is shown
    And "What needs you now" is shown
    And the card "Quality holds" is shown
    And the card "Expiring ≤ 7 d" is shown
    And the card "Inbound today" is shown

  Scenario: A card links to the screen that clears it
    When the manager opens the "Quality holds" card
    Then the URL is "/quality"
    And "Batches in quarantine" is shown
