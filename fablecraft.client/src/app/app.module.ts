import {provideHttpClient} from '@angular/common/http';
import {NgModule} from '@angular/core';
import {BrowserModule} from '@angular/platform-browser';
import {FormsModule, ReactiveFormsModule} from '@angular/forms';

import {AppRoutingModule} from './app-routing.module';
import {AppComponent} from './app.component';
import {HomeComponent} from './features/home/components/home/home.component';
import {AdventureListComponent} from './features/adventures/components/adventure-list/adventure-list.component';
import {AdventureCreateComponent} from './features/adventures/components/adventure-create/adventure-create.component';
import {AdventureStatusComponent} from './features/adventures/components/adventure-status/adventure-status.component';
import {GamePanelComponent} from './features/adventures/components/game-panel/game-panel.component';
import {
  DeleteConfirmationModalComponent
} from './features/adventures/components/delete-confirmation-modal/delete-confirmation-modal.component';
import {
  AdventureSettingsModalComponent
} from './features/adventures/components/adventure-settings-modal/adventure-settings-modal.component';
import {JsonRendererComponent} from './features/adventures/components/json-renderer/json-renderer.component';
import {ToastComponent} from './shared/components/toast/toast.component';
import {WorldbookListComponent} from './features/adventures/components/worldbook-list/worldbook-list.component';
import {WorldbookFormComponent} from './features/adventures/components/worldbook-form/worldbook-form.component';
import {MenubarComponent} from './shared/components/menubar/menubar.component';
import {
  LlmPresetManagerComponent
} from './features/adventures/components/llm-preset-manager/llm-preset-manager.component';
import {DirectoryBrowserComponent} from './shared/components/directory-browser/directory-browser.component';
import {
  LoreManagementModalComponent
} from './features/adventures/components/lore-management-modal/lore-management-modal.component';
import {
  AdventureStateModalComponent
} from './features/adventures/components/adventure-state-modal/adventure-state-modal.component';
import {LoreContentComponent} from './features/adventures/components/lore-content/lore-content.component';
import {CharactersTabComponent} from './features/adventures/components/characters-tab/characters-tab.component';
import {CharacterDetailComponent} from './features/adventures/components/character-detail/character-detail.component';
import {JsonEditorModalComponent} from './features/adventures/components/json-editor-modal/json-editor-modal.component';
import {MarkdownPipe} from './shared/pipes/markdown.pipe';

@NgModule({
  declarations: [
    AppComponent,
    HomeComponent,
    AdventureListComponent,
    AdventureCreateComponent,
    AdventureStatusComponent,
    GamePanelComponent,
    DeleteConfirmationModalComponent,
    AdventureSettingsModalComponent,
    ToastComponent,
    WorldbookListComponent,
    WorldbookFormComponent,
    MenubarComponent,
    LlmPresetManagerComponent,
    DirectoryBrowserComponent,
    LoreManagementModalComponent,
    AdventureStateModalComponent,
    LoreContentComponent,
    CharactersTabComponent,
    CharacterDetailComponent,
    JsonEditorModalComponent
  ],
  imports: [
    BrowserModule,
    AppRoutingModule,
    ReactiveFormsModule,
    FormsModule,
    JsonRendererComponent,
    MarkdownPipe
  ],
  providers: [provideHttpClient()],
  bootstrap: [AppComponent]
})
export class AppModule {
}
