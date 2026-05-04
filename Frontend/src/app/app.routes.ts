import { Routes } from '@angular/router';
import { LoginFormComponent } from './pages/login/login-form/login-form.component';
import { MainPageComponent } from './pages/main-page/main-page.component';

export const routes: Routes = [
  { path: '', component: LoginFormComponent },
  { path: 'main', component: MainPageComponent }
];
