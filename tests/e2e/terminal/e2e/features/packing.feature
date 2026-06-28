Feature: Packing (UC-11)
  As a warehouse operator in the packing zone
  I want to pack the picked items of an order and label the package
  So that the order leaves as well-formed, labelled parcels

  Background:
    Given the operator opens the packing screen

  Scenario: The packing screen shows the active package and its picked contents
    Then the active package is "PKG 1"
    And the packing detail "4006381333931" is shown

  Scenario: Opening another package for the remainder
    When the operator opens another package
    Then the active package is "PKG 2"
