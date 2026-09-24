export interface DrivingLicense {
  licenseClass: string;
  obtainedDate: string;
}

export interface Person {
  Id: string;
  firstName?: string;
  lastName?: string;
  birthDate?: string;
  licenses?: DrivingLicense[];
  assignedVehicleId?: string | null;
}
