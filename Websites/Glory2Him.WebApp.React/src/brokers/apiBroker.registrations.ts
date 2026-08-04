import { RegisterRequest, UsernameAvailability } from "../models/registrations/registration";
import ApiBroker from "./apiBroker";

class RegistrationBroker {
    relativeRegistrationsUrl = '/api/registrations';
    private apiBroker: ApiBroker = new ApiBroker();

    async GetUsernameAvailabilityAsync(userName: string): Promise<UsernameAvailability> {
        const url = `${this.relativeRegistrationsUrl}/username-available?userName=${encodeURIComponent(userName)}`;
        const result = await this.apiBroker.GetAsync(url);

        return result.data as UsernameAvailability;
    }

    async GetEmailInUseAsync(email: string): Promise<boolean> {
        const url = `${this.relativeRegistrationsUrl}/email-in-use?email=${encodeURIComponent(email)}`;
        const result = await this.apiBroker.GetAsync(url);

        return (result.data as { isInUse: boolean }).isInUse;
    }

    async GetUsernameSuggestionsAsync(name: string, surname: string, preferredName: string): Promise<string[]> {
        const parameters = new URLSearchParams({ name, surname, preferredName });
        const url = `${this.relativeRegistrationsUrl}/username-suggestions?${parameters.toString()}`;
        const result = await this.apiBroker.GetAsync(url);

        return (result.data as { suggestions: string[] }).suggestions;
    }

    async RegisterAsync(registerRequest: RegisterRequest): Promise<{ userId: string, userName: string }> {
        const result = await this.apiBroker.PostAsync(this.relativeRegistrationsUrl, registerRequest);

        return result.data as { userId: string, userName: string };
    }
}

export default RegistrationBroker;
