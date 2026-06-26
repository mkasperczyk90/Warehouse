package com.warehouse.keycloak;

import jakarta.ws.rs.core.MultivaluedMap;
import jakarta.ws.rs.core.Response;
import org.keycloak.authentication.AuthenticationFlowContext;
import org.keycloak.authentication.AuthenticationFlowError;
import org.keycloak.authentication.Authenticator;
import org.keycloak.models.KeycloakSession;
import org.keycloak.models.RealmModel;
import org.keycloak.models.UserModel;

import java.util.Optional;

/**
 * Direct-grant authenticator for the warehouse desk: a user signs in by scanning a badge number, with no
 * password. The badge arrives as a form parameter on the token request (grant_type=password); we resolve
 * the user by the {@code badge} attribute and authenticate them. Bound to the realm's direct-grant flow
 * (see the realm import). The gateway brokers the token request, so the client secret stays server-side.
 */
public class BadgeAuthenticator implements Authenticator {

    static final String BADGE_PARAM = "badge";
    static final String BADGE_ATTRIBUTE = "badge";

    @Override
    public void authenticate(AuthenticationFlowContext context) {
        MultivaluedMap<String, String> params = context.getHttpRequest().getDecodedFormParameters();
        String badge = firstNonBlank(params.getFirst(BADGE_PARAM), params.getFirst("username"));

        if (badge == null) {
            context.failure(
                AuthenticationFlowError.INVALID_CREDENTIALS,
                error(Response.Status.BAD_REQUEST, "invalid_request", "Missing badge."));
            return;
        }

        KeycloakSession session = context.getSession();
        RealmModel realm = context.getRealm();
        Optional<UserModel> user = session.users()
            .searchForUserByUserAttributeStream(realm, BADGE_ATTRIBUTE, badge.trim())
            .findFirst();

        if (user.isEmpty() || !user.get().isEnabled()) {
            context.failure(
                AuthenticationFlowError.INVALID_USER,
                error(Response.Status.UNAUTHORIZED, "invalid_grant", "Unknown or disabled badge."));
            return;
        }

        context.setUser(user.get());
        context.success();
    }

    private static String firstNonBlank(String a, String b) {
        if (a != null && !a.isBlank()) {
            return a;
        }
        return (b != null && !b.isBlank()) ? b : null;
    }

    private static Response error(Response.Status status, String error, String description) {
        return Response.status(status)
            .entity("{\"error\":\"" + error + "\",\"error_description\":\"" + description + "\"}")
            .type("application/json")
            .build();
    }

    @Override
    public void action(AuthenticationFlowContext context) {
        // Single-step direct grant: nothing to do on a follow-up action.
    }

    @Override
    public boolean requiresUser() {
        return false;
    }

    @Override
    public boolean configuredFor(KeycloakSession session, RealmModel realm, UserModel user) {
        return true;
    }

    @Override
    public void setRequiredActions(KeycloakSession session, RealmModel realm, UserModel user) {
        // No required actions for badge sign-in.
    }

    @Override
    public void close() {
        // Stateless singleton; nothing to release.
    }
}
