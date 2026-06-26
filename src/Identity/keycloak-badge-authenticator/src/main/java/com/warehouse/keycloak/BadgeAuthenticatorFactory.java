package com.warehouse.keycloak;

import org.keycloak.Config;
import org.keycloak.authentication.Authenticator;
import org.keycloak.authentication.AuthenticatorFactory;
import org.keycloak.models.AuthenticationExecutionModel.Requirement;
import org.keycloak.models.KeycloakSession;
import org.keycloak.models.KeycloakSessionFactory;
import org.keycloak.provider.ProviderConfigProperty;

import java.util.List;

/**
 * Registers {@link BadgeAuthenticator} under the provider id {@code badge-authenticator}, referenced by the
 * realm's direct-grant authentication flow. Discovered via {@code META-INF/services}.
 */
public class BadgeAuthenticatorFactory implements AuthenticatorFactory {

    public static final String PROVIDER_ID = "badge-authenticator";
    private static final BadgeAuthenticator SINGLETON = new BadgeAuthenticator();

    private static final Requirement[] REQUIREMENT_CHOICES = {
        Requirement.REQUIRED,
        Requirement.DISABLED,
    };

    @Override
    public String getId() {
        return PROVIDER_ID;
    }

    @Override
    public String getDisplayType() {
        return "Badge Authentication";
    }

    @Override
    public String getReferenceCategory() {
        return "badge";
    }

    @Override
    public boolean isConfigurable() {
        return false;
    }

    @Override
    public boolean isUserSetupAllowed() {
        return false;
    }

    @Override
    public String getHelpText() {
        return "Authenticates a warehouse desk user by scanned badge number (no password).";
    }

    @Override
    public Requirement[] getRequirementChoices() {
        return REQUIREMENT_CHOICES;
    }

    @Override
    public List<ProviderConfigProperty> getConfigProperties() {
        return List.of();
    }

    @Override
    public Authenticator create(KeycloakSession session) {
        return SINGLETON;
    }

    @Override
    public void init(Config.Scope config) {
        // No configuration.
    }

    @Override
    public void postInit(KeycloakSessionFactory factory) {
        // Nothing to initialise.
    }

    @Override
    public void close() {
        // Nothing to release.
    }
}
