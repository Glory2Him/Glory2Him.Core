import { ProfileView, UpdateProfileRequest } from "../models/profile/profileView";
import ApiBroker from "./apiBroker";

class ProfileBroker {
    relativeProfileUrl = '/api/profile';
    private apiBroker: ApiBroker = new ApiBroker();

    async GetMyProfileAsync(): Promise<ProfileView> {
        const result = await this.apiBroker.GetAsync(this.relativeProfileUrl);

        return result.data as ProfileView;
    }

    async UpdateMyProfileAsync(updateProfileRequest: UpdateProfileRequest): Promise<void> {
        await this.apiBroker.PutAsync(this.relativeProfileUrl, updateProfileRequest);
    }

    async UploadProfileImageAsync(file: File): Promise<void> {
        const formData = new FormData();
        formData.append('file', file);
        await this.apiBroker.PostFormAsync(`${this.relativeProfileUrl}/image`, formData);
    }

    async DeleteProfileImageAsync(): Promise<void> {
        await this.apiBroker.DeleteAsync(`${this.relativeProfileUrl}/image`);
    }
}

export default ProfileBroker;
