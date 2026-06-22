Feature: Dispatch to carrier (UC-12)
  As a logistics coordinator
  I want a board of shipments by status and to assign carriers
  So that packed orders are handed to a carrier and tracked to dispatch

  Background:
    Given the coordinator opens the dispatch board

  Scenario: The board shows the four status columns
    Then the dispatch board is shown
    And "Carrier assigned" is shown
    And "Pickup notice sent" is shown
    And "Dispatched" is shown

  Scenario: Dispatched shipments carry their tracking and a waybill
    Then "Tracking GLS-PL-99213 · waybill issued" is shown

  Scenario: Assigning a carrier moves the shipment off the packed queue
    Then there are 2 assign-carrier actions
    When the coordinator assigns a carrier "DH" to the first packed shipment
    Then there is 1 assign-carrier action

  Scenario: Filtering the board by carrier
    When the coordinator filters the board by carrier "GLS"
    Then shipment "SHP-3302" is shown
    And shipment "SHP-3301" is not shown
