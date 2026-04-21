import { Routes } from '@angular/router';
import { LoginFormComponent } from './login/login-form/login-form';
import { MainPageComponent } from './main-page/main-page';

export const routes: Routes = [
  { path: '', component: LoginFormComponent },
  { path: 'main', component: MainPageComponent }
];
