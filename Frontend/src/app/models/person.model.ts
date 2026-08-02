export interface DrivingLicense {
  licenseClass: string;
  obtainedDate: string;
}

export interface Person {
  // Server-generated UUID — also displayed as the "Mitarbeiter-Nr.", there is no
  // separate employee number field.
  Id: string;
  firstName?: string;
  lastName?: string;
  birthDate?: string;
  licenses?: DrivingLicense[];
  assignedVehicleId?: string | null;
}
