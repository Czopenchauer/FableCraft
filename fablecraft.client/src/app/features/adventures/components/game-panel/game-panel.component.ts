import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

@Component({
  selector: 'app-game-panel',
  standalone: false,
  templateUrl: './game-panel.component.html',
  styleUrl: './game-panel.component.css'
})
export class GamePanelComponent implements OnInit {
  adventureId: string | null = null;

  constructor(
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.adventureId = this.route.snapshot.paramMap.get('id');

    if (!this.adventureId) {
      // Redirect to adventures list if no ID provided
      this.router.navigate(['/adventures']);
    }
  }

  goToAdventureList(): void {
    this.router.navigate(['/adventures']);
  }
}
