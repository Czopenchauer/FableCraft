import {provideHttpClient} from '@angular/common/http';
import {NgModule} from '@angular/core';
import {BrowserModule} from '@angular/platform-browser';
import {ReactiveFormsModule, FormsModule} from '@angular/forms';

import {AppRoutingModule} from './app-routing.module';
import {AppComponent} from './app.component';
import { HomeComponent } from './features/home/components/home/home.component';
import { AdventureListComponent } from './features/adventures/components/adventure-list/adventure-list.component';
import { AdventureCreateComponent } from './features/adventures/components/adventure-create/adventure-create.component';
import { AdventureStatusComponent } from './features/adventures/components/adventure-status/adventure-status.component';
import { GamePanelComponent } from './features/adventures/components/game-panel/game-panel.component';

@NgModule({
  declarations: [
    AppComponent,
    HomeComponent,
    AdventureListComponent,
    AdventureCreateComponent,
    AdventureStatusComponent,
    GamePanelComponent
  ],
  imports: [
    BrowserModule,
    AppRoutingModule,
    ReactiveFormsModule,
    FormsModule
  ],
  providers: [provideHttpClient()],
  bootstrap: [AppComponent]
})
export class AppModule {
}
