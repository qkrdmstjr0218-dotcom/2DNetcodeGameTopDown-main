mergeInto(LibraryManager.library, {
  ArenaTextClose: function () {
    if (Module.arenaTextInput) Module.arenaTextInput.close();
  },
  ArenaTextOpen: function (receiverPtr, requestPtr) {
    if (Module.arenaTextInput) Module.arenaTextInput.close();
    var receiver = UTF8ToString(receiverPtr), request = JSON.parse(UTF8ToString(requestPtr));
    var overlay = document.createElement('div'), panel = document.createElement('form');
    var title = document.createElement('label'), hint = document.createElement('p');
    var input = document.createElement('input'), actions = document.createElement('div');
    var save = document.createElement('button'), cancel = document.createElement('button');
    var composing = false, endedAt = -Infinity, pending = false, closed = false;
    overlay.style.cssText = 'position:fixed;inset:0;z-index:2147483647;display:flex;align-items:center;justify-content:center;background:rgba(0,8,12,.8);font:16px sans-serif;color:#bed7dd;';
    panel.style.cssText = 'box-sizing:border-box;width:440px;max-width:calc(100vw - 32px);padding:24px;background:#08151b;border:1px solid #47717e;box-shadow:0 12px 48px #0008;';
    panel.setAttribute('role','dialog');panel.setAttribute('aria-modal','true');panel.setAttribute('aria-labelledby','arena-text-title');
    title.id = 'arena-text-title';title.htmlFor = 'arena-browser-text';title.textContent = request.title;
    title.style.cssText = 'display:block;margin:0 0 18px;font-size:22px;color:#83dce4;';
    input.id = 'arena-browser-text';input.type = 'text';input.value = request.text || '';input.maxLength = 128;
    input.autocomplete = 'off';input.spellcheck = false;
    input.style.cssText = 'box-sizing:border-box;width:100%;padding:12px;border:1px solid #5a9aa8;background:#102731;color:#eef9fb;font:18px sans-serif;outline-color:#67d4df;';
    hint.textContent = request.hint;hint.style.cssText = 'font-size:14px;line-height:1.6;color:#97b7c0;margin:12px 0 24px;';
    actions.style.cssText = 'display:flex;gap:10px;';
    save.type = 'submit';save.textContent = request.title === '닉네임' ? '저장' : '적용';
    cancel.type = 'button';cancel.textContent = '취소';
    [save,cancel].forEach(function (button) {button.style.cssText = 'flex:1;padding:10px;cursor:pointer;border:1px solid #47717e;background:#173540;color:#c8e6eb;font:16px sans-serif;';actions.appendChild(button);});
    panel.appendChild(title);panel.appendChild(input);panel.appendChild(hint);panel.appendChild(actions);overlay.appendChild(panel);
    var close = function () {
      if (closed) return;closed = true;overlay.remove();
      if (Module.arenaTextInput && Module.arenaTextInput.close === close) Module.arenaTextInput = null;
      if (Module.canvas) Module.canvas.focus();
    };
    var finish = function (submitted) {
      if (closed) return;
      var result = JSON.stringify({ticket:request.ticket,submitted:submitted,text:input.value});close();
      SendMessage(receiver,'OnBrowserTextResult',result);
    };
    input.addEventListener('compositionstart',function () {composing = true;});
    input.addEventListener('compositionend',function () {
      composing = false;endedAt = performance.now();
      // A click during composition may blur/commit after the submit event.
      if (pending) setTimeout(function () {if (!composing) finish(true);},0);
    });
    panel.addEventListener('submit',function (event) {
      event.preventDefault();
      if (composing) {pending = true;input.blur();return;}
      finish(true);
    });
    cancel.addEventListener('click',function () {finish(false);});
    overlay.addEventListener('keydown',function (event) {
      if (event.key === 'Escape' && !composing && !event.isComposing) {event.preventDefault();finish(false);}
      // Enter used to confirm a Hangul syllable must not submit the form.
      if (event.key === 'Enter' && (composing || event.isComposing || event.keyCode === 229 || performance.now()-endedAt < 80)) event.preventDefault();
      if (event.key === 'Tab') {
        var controls = [input,save,cancel], index = controls.indexOf(document.activeElement);
        if (event.shiftKey && index === 0) {event.preventDefault();cancel.focus();}
        else if (!event.shiftKey && index === 2) {event.preventDefault();input.focus();}
      }
      event.stopPropagation();
    });
    ['keyup','keypress'].forEach(function (type) {overlay.addEventListener(type,function (event) {event.stopPropagation();});});
    // Fullscreen elements own a separate top layer: mount inside it when present.
    var mount = function () {
      if (closed) return;
      (document.fullscreenElement || document.body).appendChild(overlay);
      input.focus();input.select();
    };
    Module.arenaTextInput = {close:close};
    // A canvas cannot render DOM children. Leave canvas-only fullscreen first.
    if (document.fullscreenElement === Module.canvas && document.exitFullscreen) document.exitFullscreen().then(mount,function () {finish(false);});
    else mount();
  }
});
