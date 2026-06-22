Feature: Sidebar navigation
  As a warehouse desk manager
  I want a persistent sidebar across every section
  So that I can move between Inventory, Logistics and Master data at any time

  Background:
    Given the manager opens the admin panel

  Scenario: The panel lands on the Today worklist
    Then the Today worklist is shown

  Scenario: Navigating to the Stock view
    When the manager opens the "Stock view" section
    Then the Stock view is shown

  Scenario: Navigating between sections
    When the manager opens the "Movements" section
    Then the URL is "/movements"
    And the heading "Stock movements" is shown
    When the manager opens the "Inbound (ASN)" section
    Then the URL is "/inbound"
    And "Announced deliveries" is shown
    When the manager opens the "Quality holds" section
    Then the URL is "/quality"
    And "Batches in quarantine" is shown

  Scenario: An unbuilt section is not navigable
    Then the "Partners" section is disabled
