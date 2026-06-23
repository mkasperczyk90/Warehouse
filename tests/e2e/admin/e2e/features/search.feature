Feature: Global search (the top-bar command bar)
  As a warehouse desk manager
  I want to search anything from the top bar
  So that I can jump straight to a product, stock item or ASN

  Background:
    Given the manager opens the admin panel

  Scenario: Searching surfaces matching results
    When the manager searches globally for "milk"
    Then the search result "Whole milk 3.2% — 1 L carton" is shown

  Scenario: Opening a result jumps to it
    When the manager searches globally for "ASN-2206"
    And the manager opens the search result "ASN-2206"
    Then the URL is "/inbound"
    And "Announced deliveries" is shown
