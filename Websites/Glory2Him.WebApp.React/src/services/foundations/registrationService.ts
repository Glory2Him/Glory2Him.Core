import { useMutation } from '@tanstack/react-query';
import RegistrationBroker from "../../brokers/apiBroker.registrations";
import { RegisterRequest } from "../../models/registrations/registration";

export const registrationService = {
    useRegister: () => {
        const registrationBroker = new RegistrationBroker();

        return useMutation({
            mutationFn: async (request: RegisterRequest) =>
                await registrationBroker.RegisterAsync(request)
        });
    },

    useCheckUsername: () => {
        const registrationBroker = new RegistrationBroker();

        return useMutation({
            mutationFn: async (userName: string) =>
                await registrationBroker.GetUsernameAvailabilityAsync(userName)
        });
    },

    useCheckEmailInUse: () => {
        const registrationBroker = new RegistrationBroker();

        return useMutation({
            mutationFn: async (email: string) =>
                await registrationBroker.GetEmailInUseAsync(email)
        });
    },

    useGetUsernameSuggestions: () => {
        const registrationBroker = new RegistrationBroker();

        return useMutation({
            mutationFn: async (input: { name: string, surname: string, preferredName: string }) =>
                await registrationBroker.GetUsernameSuggestionsAsync(input.name, input.surname, input.preferredName)
        });
    }
};
