Feature: Sign in by badge scan
  As a warehouse desk user
  I want to sign in by scanning my badge
  So that the panel opens scoped to me and my warehouse

  Background:
    Given the desk shows the login screen

  Scenario: A known badge signs the manager in
    When the user scans badge "1001"
    Then the Today worklist is shown

  Scenario: An unknown badge is rejected
    When the user scans badge "9999"
    Then the login error is shown
    And the login screen is still shown
