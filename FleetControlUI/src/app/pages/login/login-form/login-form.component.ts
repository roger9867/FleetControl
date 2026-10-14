import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

@Component({
  selector: 'app-login-form',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './login-form.component.html',
  styleUrl: './login-form.component.scss'
})
export class LoginFormComponent {
  email: string = '';
  password: string = '';

  showPassword: boolean = false;

  // Injects the router for navigation after login
  constructor(private router: Router) {}

  // Toggles the visibility of the password field
  togglePassword() {
    this.showPassword = !this.showPassword;
  }

  // Processes the login form and navigates to the main page
  onSubmit() {
    console.log('Login:', this.email, this.password);
    this.router.navigate(['/main']);
  }
}
