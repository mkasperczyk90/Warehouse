Feature: Inbound deliveries (UC-01 Announce delivery, UC-02 Receive)
  As a logistics coordinator
  I want to announce, slot and receive incoming deliveries
  So that the warehouse knows what arrives and can take it onto stock

  Background:
    Given the coordinator opens the inbound deliveries

  Scenario: The announced deliveries and the first ASN's lines are shown
    Then the inbound list is shown
    And "ASN-2206" is shown
    And "Whole milk 3.2% 1 L" is shown

  Scenario: An unknown SKU line is flagged for clarification (UC-01 exception)
    Then "Unknown SKU 9900001 — flagged for clarification" is shown

  Scenario: Creating an ASN needs a supplier and at least one line (UC-01 step 1)
    When the coordinator starts a new ASN
    And the coordinator enters the supplier "New Supplier Co"
    Then the create-ASN button is disabled
    When the coordinator adds a line with SKU "5900000000001" and quantity 100
    Then the create-ASN button is enabled
    When the coordinator submits the ASN
    Then "— New Supplier Co" is shown

  Scenario: Assigning a dock slot to an ASN (UC-01 step 3)
    When the coordinator selects ASN "ASN-2208"
    Then "ASN-2208 — ACME Packaging" is shown
    When the coordinator assigns dock "D-2" with window "11:00–12:00"
    Then "D-2 · 11:00–12:00" is shown

  Scenario: Marking an announced ASN as arrived (lifecycle Announced → Arrived)
    When the coordinator selects ASN "ASN-2207"
    Then "ASN-2207 — Nordic Frozen AS" is shown
    When the coordinator marks the ASN as arrived
    Then the mark-arrived button is gone

  Scenario: Resolving an unknown-SKU line (UC-01 exception)
    When the coordinator resolves the flagged line to SKU "5901111000048" product "Stretch film 500 mm"
    Then "Stretch film 500 mm" is shown
    And "Unknown SKU 9900001 — flagged for clarification" is no longer shown

  Scenario: Viewing receiving progress for an arrived ASN (UC-02)
    When the coordinator opens the receiving view
    Then the URL is "/inbound/ASN-2206/receiving"
    And "Receiving — ASN-2206" is shown
