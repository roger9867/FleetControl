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

  // Injiziert den Router fuer die Navigation nach dem Login
  constructor(private router: Router) {}

  // Schaltet die Sichtbarkeit des Passwort-Felds um
  togglePassword() {
    this.showPassword = !this.showPassword;
  }

  // Verarbeitet das Login-Formular und navigiert zur Hauptseite
  onSubmit() {
    console.log('Login:', this.email, this.password);
    this.router.navigate(['/main']);
  }
}
