// Progressive enhancement only: every page works with this file switched off.
(function () {
  'use strict';
  document.documentElement.classList.add('js');

  // Mobile menu
  var toggle = document.querySelector('.nav-toggle');
  var nav = document.getElementById('site-nav');
  if (toggle && nav) {
    toggle.addEventListener('click', function () {
      var open = nav.classList.toggle('open');
      toggle.setAttribute('aria-expanded', open ? 'true' : 'false');
    });
  }

  // Never leave a broken image on screen.
  document.addEventListener('error', function (event) {
    var img = event.target;
    if (img && img.tagName === 'IMG' && img.dataset.fallback && !img.dataset.failed) {
      img.dataset.failed = '1';
      img.src = img.dataset.fallback;
    }
  }, true);

  // Bike detail: a colour swatch swaps the main photo.
  var picker = document.querySelector('[data-colour-picker]');
  var stage = document.querySelector('[data-colour-stage] img');
  if (picker && stage) {
    var colourName = picker.querySelector('[data-colour-name]');
    var colourNote = picker.querySelector('[data-colour-note]');
    var swatches = Array.prototype.slice.call(picker.querySelectorAll('[data-colour]'));
    stage.addEventListener('load', function () { stage.classList.remove('is-loading'); });
    stage.addEventListener('error', function () { stage.classList.remove('is-loading'); });
    var choose = function (swatch) {
      swatches.forEach(function (s) { s.setAttribute('aria-pressed', s === swatch ? 'true' : 'false'); });
      colourName.textContent = swatch.dataset.colour;
      colourNote.hidden = swatch.dataset.photo === '1';
      stage.alt = picker.dataset.alt + (swatch.dataset.photo === '1' ? ' in ' + swatch.dataset.colour : '');
      if (stage.getAttribute('src') === swatch.dataset.src) return;
      delete stage.dataset.failed;
      stage.classList.add('is-loading');
      stage.src = swatch.dataset.src;
    };
    picker.addEventListener('click', function (event) {
      var swatch = event.target.closest('[data-colour]');
      if (swatch) choose(swatch);
    });
    // Warm the cache on hover so the switch is instant.
    picker.addEventListener('pointerover', function (event) {
      var swatch = event.target.closest('[data-colour]');
      if (swatch && !swatch.dataset.warmed) { swatch.dataset.warmed = '1'; new Image().src = swatch.dataset.src; }
    });
  }

  // Brand select narrows the model select beside it.
  function wirePicker(scope) {
    var brand = scope.querySelector('[data-brand-select]');
    var model = scope.querySelector('[data-model-select]');
    if (!brand || !model) return;
    var groups = Array.prototype.slice.call(model.querySelectorAll('optgroup'));
    function apply() {
      // Mobile browsers ignore hidden on optgroup, so other brands' groups are taken out of the list.
      var current = model.value;
      groups.forEach(function (group) {
        if (group.parentNode) model.removeChild(group);
        if (!brand.value || group.dataset.brand === brand.value) model.appendChild(group);
      });
      model.value = current;
      if (model.selectedIndex < 0) model.value = '';
    }
    var preset = model.selectedOptions[0];
    if (preset && preset.dataset.brand && !brand.value) brand.value = preset.dataset.brand;
    brand.addEventListener('change', function () { apply(); model.dispatchEvent(new Event('change')); });
    apply();
  }
  Array.prototype.forEach.call(document.querySelectorAll('[data-bike-picker], [data-bike-search]'), wirePicker);

  // Home search: Brand, Brand + Model, or Brand + Budget.
  var search = document.querySelector('[data-bike-search]');
  if (search) {
    var sBrand = search.querySelector('[data-brand-select]');
    var sModel = search.querySelector('[data-model-select]');
    var sBudget = search.querySelector('[data-budget-select]');
    var button = search.querySelector('[data-search-button]');
    var refresh = function () {
      sModel.disabled = !sBrand.value || !!sBudget.value;
      sBudget.disabled = !sBrand.value || !!sModel.value;
      button.disabled = !sBrand.value;
    };
    [sBrand, sModel, sBudget].forEach(function (el) { el.addEventListener('change', refresh); });
    refresh();
  }

  // Searchable dropdowns: a list with a search box drawn over the real select, which stays the source of truth.
  var openCombo = null;
  function wireCombo(select) {
    var wrap = document.createElement('div');
    var button = document.createElement('button');
    var panel = document.createElement('div');
    var input = document.createElement('input');
    var list = document.createElement('ul');
    var empty = document.createElement('p');
    var label = document.querySelector('label[for="' + select.id + '"]');
    var name = label ? label.textContent.trim() : '';
    var items = [];
    var active = -1;
    var combo = {};

    wrap.className = 'combo';
    select.parentNode.insertBefore(wrap, select);
    select.classList.add('combo-native');
    button.type = 'button';
    button.id = select.id + '-combo';
    button.className = 'combo-btn';
    button.setAttribute('aria-haspopup', 'listbox');
    button.setAttribute('aria-expanded', 'false');
    panel.className = 'combo-panel';
    panel.hidden = true;
    input.type = 'search';
    input.className = 'combo-search';
    input.placeholder = 'Search ' + name.toLowerCase();
    input.autocomplete = 'off';
    input.setAttribute('role', 'combobox');
    input.setAttribute('aria-label', input.placeholder);
    input.setAttribute('aria-expanded', 'true');
    input.setAttribute('aria-autocomplete', 'list');
    input.setAttribute('aria-controls', select.id + '-combo-list');
    list.id = select.id + '-combo-list';
    list.className = 'combo-list';
    list.setAttribute('role', 'listbox');
    list.setAttribute('aria-label', name);
    empty.className = 'combo-empty';
    empty.setAttribute('role', 'status');
    empty.hidden = true;
    panel.appendChild(input);
    panel.appendChild(list);
    panel.appendChild(empty);
    wrap.appendChild(select);
    wrap.appendChild(button);
    wrap.appendChild(panel);
    if (label) label.htmlFor = button.id;

    function sync() {
      var option = select.options[select.selectedIndex];
      button.textContent = option ? option.textContent : '';
      button.classList.toggle('is-placeholder', !select.value);
      button.disabled = select.disabled;
      if (select.disabled && openCombo === combo) combo.close();
    }

    function setActive(index) {
      if (items[active]) items[active].classList.remove('is-active');
      active = index;
      var item = items[active];
      if (!item) {
        input.removeAttribute('aria-activedescendant');
        return;
      }
      item.classList.add('is-active');
      input.setAttribute('aria-activedescendant', item.id);
      if (item.offsetTop < list.scrollTop) list.scrollTop = item.offsetTop;
      else if (item.offsetTop + item.offsetHeight > list.scrollTop + list.clientHeight) list.scrollTop = item.offsetTop + item.offsetHeight - list.clientHeight;
    }

    function render() {
      var words = input.value.toLowerCase().split(/\s+/).filter(Boolean);
      var chosen = -1;
      items = [];
      active = -1;
      list.textContent = '';
      function addOption(option, group) {
        // The blank choice ("Any model") only makes sense when nothing has been typed.
        if (!option.value && words.length) return null;
        var text = ((group ? group.label + ' ' : '') + option.textContent).toLowerCase();
        if (!words.every(function (word) { return text.indexOf(word) !== -1; })) return null;
        var item = document.createElement('li');
        item.id = select.id + '-combo-' + items.length;
        item.className = 'combo-option';
        item.textContent = option.textContent;
        item.setAttribute('role', 'option');
        item.setAttribute('aria-selected', option.selected ? 'true' : 'false');
        item.option = option;
        if (option.selected) chosen = items.length;
        items.push(item);
        return item;
      }
      Array.prototype.forEach.call(select.children, function (child) {
        if (child.tagName === 'OPTGROUP') {
          var found = Array.prototype.map.call(child.children, function (option) { return addOption(option, child); }).filter(Boolean);
          if (found.length) {
            var heading = document.createElement('li');
            heading.className = 'combo-group';
            heading.textContent = child.label;
            heading.setAttribute('role', 'presentation');
            list.appendChild(heading);
            found.forEach(function (item) { list.appendChild(item); });
          }
        } else {
          var item = addOption(child, null);
          if (item) list.appendChild(item);
        }
      });
      empty.hidden = items.length > 0;
      empty.textContent = items.length ? '' : 'No matches found';
      list.hidden = items.length === 0;
      list.scrollTop = 0;
      setActive(words.length ? (items.length ? 0 : -1) : chosen);
    }

    function choose(item) {
      item.option.selected = true;
      wrap.classList.remove('is-invalid');
      select.dispatchEvent(new Event('change', { bubbles: true }));
      combo.close();
      button.focus();
    }

    combo.close = function () {
      if (openCombo === combo) openCombo = null;
      panel.hidden = true;
      wrap.classList.remove('is-open', 'combo-up');
      button.setAttribute('aria-expanded', 'false');
    };

    combo.open = function () {
      if (select.disabled) return;
      if (openCombo) openCombo.close();
      openCombo = combo;
      input.value = '';
      panel.hidden = false;
      wrap.classList.add('is-open');
      button.setAttribute('aria-expanded', 'true');
      render();
      // On phones keep the list above the on-screen keyboard; on larger screens open upwards if there is no room below.
      if (window.matchMedia('(max-width: 719px)').matches) {
        wrap.scrollIntoView({ block: 'start' });
      } else {
        var box = wrap.getBoundingClientRect();
        var below = window.innerHeight - box.bottom;
        wrap.classList.toggle('combo-up', below < panel.offsetHeight + 12 && box.top > below);
      }
      input.focus();
    };

    button.addEventListener('click', function () {
      if (openCombo === combo) combo.close();
      else combo.open();
    });
    button.addEventListener('keydown', function (event) {
      if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
        event.preventDefault();
        combo.open();
      }
    });
    input.addEventListener('input', render);
    input.addEventListener('keydown', function (event) {
      if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
        event.preventDefault();
        if (!items.length) return;
        var step = event.key === 'ArrowDown' ? 1 : -1;
        setActive(active === -1 ? (step === 1 ? 0 : items.length - 1) : (active + step + items.length) % items.length);
      } else if (event.key === 'Enter') {
        event.preventDefault();
        if (items[active]) choose(items[active]);
      } else if (event.key === 'Escape') {
        event.preventDefault();
        event.stopPropagation();
        combo.close();
        button.focus();
      } else if (event.key === 'Tab') {
        combo.close();
        button.focus();
      }
    });
    // Keep focus in the search box so the phone keyboard stays up while the list is tapped.
    list.addEventListener('mousedown', function (event) { event.preventDefault(); });
    list.addEventListener('click', function (event) {
      var item = event.target.closest('[role="option"]');
      if (item) choose(item);
    });
    select.addEventListener('change', sync);
    // A required choice cannot show the browser's message on a hidden select, so mark it and open it.
    select.addEventListener('invalid', function (event) {
      event.preventDefault();
      wrap.classList.add('is-invalid');
      if (!openCombo) combo.open();
    });
    // The select also changes from code: wirePicker swaps its models and the home search disables it.
    new MutationObserver(sync).observe(select, { childList: true, attributes: true, attributeFilter: ['disabled'] });
    window.addEventListener('pageshow', sync);
    combo.wrap = wrap;
    sync();
  }
  Array.prototype.forEach.call(document.querySelectorAll('select[data-searchable]'), wireCombo);
  document.addEventListener('pointerdown', function (event) {
    if (openCombo && !openCombo.wrap.contains(event.target)) openCombo.close();
  });

  // EMI calculator: live result as the values change.
  var emi = document.querySelector('[data-emi-form]');
  if (emi) {
    var rupees = function (n) { return '₹' + Math.round(n).toLocaleString('en-IN'); };
    var set = function (key, value) {
      var el = document.querySelector('[data-emi="' + key + '"]');
      if (el) el.textContent = rupees(value);
    };
    var calculate = function () {
      if (!emi.checkValidity()) return;
      var p = Number(emi.elements.amount.value);
      var r = Number(emi.elements.rate.value) / 1200;
      var n = Number(emi.elements.years.value) * 12;
      var growth = Math.pow(1 + r, n);
      var monthly = p * r * growth / (growth - 1);
      set('monthly', monthly);
      set('principal', p);
      set('interest', monthly * n - p);
      set('total', monthly * n);
    };
    emi.addEventListener('input', calculate);
  }

  // Full EMI calculator page: sliders, donut, breakdown and yearly schedule.
  var calc = document.querySelector('[data-emi-calculator]');
  if (calc) {
    var inr = function (n) { return '₹' + Math.round(n).toLocaleString('en-IN'); };
    var amount = calc.elements.amount;
    var minAmount = Number(amount.min), maxAmount = Number(amount.max);
    // The amount slider is logarithmic so that ₹50K–₹5L, where most bike loans sit, gets most of the track.
    var toLog = function (v) { return Math.round(1000 * Math.log(v / minAmount) / Math.log(maxAmount / minAmount)); };
    var fromLog = function (t) {
      var v = minAmount * Math.pow(maxAmount / minAmount, t / 1000);
      var step = v < 1000000 ? 1000 : 10000;
      return Math.min(maxAmount, Math.max(minAmount, Math.round(v / step) * step));
    };
    var sliders = Array.prototype.slice.call(calc.querySelectorAll('[data-emi-slider]'));
    var syncSliders = function () {
      sliders.forEach(function (slider) {
        var field = calc.elements[slider.dataset.emiSlider];
        if (!field.checkValidity() || field.value === '') return;
        slider.value = slider.dataset.scale === 'log' ? toLog(Number(field.value)) : field.value;
      });
    };
    sliders.forEach(function (slider) {
      slider.addEventListener('input', function (event) {
        event.stopPropagation();
        var field = calc.elements[slider.dataset.emiSlider];
        field.value = slider.dataset.scale === 'log' ? fromLog(Number(slider.value)) : slider.value;
        update();
      });
    });

    var out = function (key, text) {
      Array.prototype.forEach.call(calc.ownerDocument.querySelectorAll('[data-emi-out="' + key + '"]'), function (el) { el.textContent = text; });
    };
    var svg = function (name, attrs) {
      var el = document.createElementNS('http://www.w3.org/2000/svg', name);
      Object.keys(attrs).forEach(function (k) { el.setAttribute(k, attrs[k]); });
      return el;
    };
    var cell = function (tag, text) { var el = document.createElement(tag); el.textContent = text; return el; };
    var donut = document.querySelector('[data-emi-donut]');
    var schedule = document.querySelector('[data-emi-schedule]');

    var update = function () {
      if (!calc.checkValidity()) return;
      var p = Number(amount.value);
      var ratePct = Number(calc.elements.rate.value);
      var years = Number(calc.elements.years.value);
      var feePct = Number(calc.elements.fee.value || 0);
      var r = ratePct / 1200;
      var n = years * 12;
      var growth = Math.pow(1 + r, n);
      var monthly = p * r * growth / (growth - 1);
      var total = Math.round(monthly * n);
      var interest = total - p;
      var fee = Math.round(p * feePct / 100);
      var share = total ? Math.round(interest * 100 / total) : 0;

      out('monthly', inr(monthly));
      out('months', String(n));
      out('principal', inr(p));
      out('interest', inr(interest));
      out('total', inr(total));
      out('fee', inr(fee));
      out('cost', inr(interest + fee));
      out('rate', String(ratePct));
      out('interest-share', share + '%');
      out('principal-share', (100 - share) + '%');
      if (donut) donut.setAttribute('stroke-dasharray', share + ' 100');

      if (schedule) {
        var rows = document.createDocumentFragment();
        var balance = p;
        for (var year = 1; year <= years; year++) {
          var paidInterest = 0, paidPrincipal = 0;
          for (var m = 0; m < 12; m++) {
            var due = balance * r;
            paidInterest += due;
            paidPrincipal += monthly - due;
            balance -= monthly - due;
          }
          var width = Math.round(paidPrincipal * 1000 / (paidPrincipal + paidInterest)) / 10;
          var tr = document.createElement('tr');
          var head = cell('th', 'Year ' + year);
          head.setAttribute('scope', 'row');
          tr.appendChild(head);
          tr.appendChild(cell('td', inr(paidPrincipal)));
          tr.appendChild(cell('td', inr(paidInterest)));
          tr.appendChild(cell('td', inr(Math.max(0, balance))));
          var barCell = document.createElement('td');
          barCell.className = 'ec-bar-col';
          var bar = svg('svg', { 'class': 'ec-bar', viewBox: '0 0 100 10', preserveAspectRatio: 'none', 'aria-hidden': 'true', focusable: 'false' });
          bar.appendChild(svg('rect', { 'class': 'ec-bar-principal', x: 0, y: 0, height: 10, width: width }));
          bar.appendChild(svg('rect', { 'class': 'ec-bar-interest', x: width, y: 0, height: 10, width: 100 - width }));
          barCell.appendChild(bar);
          tr.appendChild(barCell);
          rows.appendChild(tr);
        }
        schedule.replaceChildren(rows);
      }

      // Keep the address shareable without reloading the page.
      var query = '?amount=' + p + '&rate=' + ratePct + '&years=' + years + (feePct ? '&fee=' + feePct : '');
      history.replaceState(null, '', calc.getAttribute('action') + query);
    };

    calc.addEventListener('input', function () { syncSliders(); update(); });
    calc.addEventListener('submit', function (event) { event.preventDefault(); update(); });
    syncSliders();
  }

  // Electric running cost: same formulas as RunningCost.Calculate on the server.
  var ev = document.querySelector('[data-ev-calculator]');
  if (ev) {
    var money = function (n) { return (n < 0 ? '−' : '') + '₹' + Math.round(Math.abs(n)).toLocaleString('en-IN'); };
    var perKm = function (n) { return '₹' + n.toFixed(2); };
    var show = function (key, text) {
      Array.prototype.forEach.call(document.querySelectorAll('[data-ev-out="' + key + '"]'), function (el) { el.textContent = text; });
    };
    var battery = Number(ev.dataset.battery);
    var evPrice = ev.dataset.evPrice ? Number(ev.dataset.evPrice) : null;
    var vs = ev.querySelector('[data-ev-vs]');
    var saving = document.querySelector('[data-ev-saving]');
    var vehicle = ev.dataset.vehicle;
    var presets = Array.prototype.slice.call(ev.querySelectorAll('[data-ev-range]'));
    var evSliders = Array.prototype.slice.call(ev.querySelectorAll('[data-ev-slider]'));

    var syncEv = function () {
      evSliders.forEach(function (s) { var f = ev.elements[s.dataset.evSlider]; if (f.checkValidity()) s.value = f.value; });
      presets.forEach(function (b) { b.setAttribute('aria-pressed', b.dataset.evRange === ev.elements.range.value ? 'true' : 'false'); });
    };
    var payback = function (months) {
      if (months === null) return 'Does not pay back at these numbers';
      if (months === 0) return 'Costs no more to buy';
      if (months === 1) return 'About 1 month';
      if (months < 12) return 'About ' + months + ' months';
      return 'About ' + Math.floor(months / 12) + ' yr ' + (months % 12) + ' mo';
    };

    var recalc = function () {
      if (!ev.checkValidity()) return;
      var f = ev.elements;
      var km = Number(f.km.value), days = Number(f.days.value), range = Number(f.range.value);
      var perCharge = battery / (1 - Number(f.loss.value) / 100) * Number(f.tariff.value);
      var kmMonth = km * days;
      var evKm = perCharge / range;
      var evMonthly = evKm * kmMonth;
      show('ev-monthly', money(evMonthly));
      show('ev-km', perKm(Math.round(evKm * 100) / 100));
      show('km-month', String(kmMonth));
      show('per-charge', money(perCharge));
      show('charges', String(Math.round(kmMonth / range * 10) / 10));

      if (vs) {
        var option = vs.selectedOptions[0];
        var petrolKm = Number(f.petrol.value) / Number(option.dataset.kmpl);
        var petrolMonthly = petrolKm * kmMonth;
        var monthlySaving = Math.round(petrolMonthly - evMonthly);
        show('petrol-monthly', money(petrolMonthly));
        show('petrol-km', perKm(Math.round(petrolKm * 100) / 100));
        show('saving-year', money(monthlySaving * 12));

        var five = { ev: Math.round(evMonthly) * 60, petrol: Math.round(petrolMonthly) * 60 };
        var max = Math.max(1, five.ev, five.petrol);
        Object.keys(five).forEach(function (k) {
          var bar = document.querySelector('[data-ev-bar="' + k + '"]');
          if (bar) bar.setAttribute('width', Math.round(five[k] * 1000 / max) / 10);
          show(k + '-5y', money(five[k]));
        });

        if (saving) {
          var strong = document.createElement('strong');
          strong.textContent = money(Math.abs(monthlySaving));
          saving.classList.toggle('ev-worse', monthlySaving < 0);
          saving.replaceChildren(
            monthlySaving >= 0 ? 'You save ' : 'The electric ' + vehicle + ' costs ', strong,
            monthlySaving >= 0 ? ' a month compared with the petrol ' + vehicle + '.' : ' a month more to run at these numbers.');
        }

        var petrolPrice = Number(option.dataset.price);
        if (evPrice !== null && petrolPrice) {
          show('price-gap', money(evPrice - petrolPrice));
          show('payback', payback(monthlySaving > 0 ? (evPrice <= petrolPrice ? 0 : Math.ceil((evPrice - petrolPrice) / monthlySaving)) : null));
        }
      }

      var params = new URLSearchParams(new FormData(ev));
      history.replaceState(null, '', ev.getAttribute('action') + '?' + params.toString());
    };

    evSliders.forEach(function (s) {
      s.addEventListener('input', function (event) {
        event.stopPropagation();
        ev.elements[s.dataset.evSlider].value = s.value;
        syncEv();
        recalc();
      });
    });
    presets.forEach(function (b) {
      b.addEventListener('click', function () { ev.elements.range.value = b.dataset.evRange; syncEv(); recalc(); });
    });
    ev.addEventListener('input', function () { syncEv(); recalc(); });
    ev.addEventListener('submit', function (event) { event.preventDefault(); recalc(); });
    syncEv();
  }

  // Wishlist. Saved in this browser; for a signed-in account (when the site allows database writes) also in the
  // Wishlist table, which then becomes the source of truth.
  (function () {
    var KEY = 'bch.wishlist', OWNER = 'bch.wishlist.owner', MAX = 50;
    var userId = document.body.dataset.wishlistUser || '';
    var read = function () {
      try {
        var v = JSON.parse(localStorage.getItem(KEY) || '[]');
        return Array.isArray(v) ? v.filter(function (x) { return x && typeof x.s === 'string'; }) : [];
      } catch (e) { return []; }
    };
    var owner = function (value) {
      try {
        if (value === undefined) return localStorage.getItem(OWNER) || '';
        if (value) localStorage.setItem(OWNER, value); else localStorage.removeItem(OWNER);
      } catch (e) { /* storage blocked: the list just won't persist */ }
      return '';
    };
    var list = read();
    var has = function (slug) { return list.some(function (x) { return x.s === slug; }); };
    var slugs = function () { return list.map(function (x) { return x.s; }); };
    var listeners = [];
    var save = function () {
      try { localStorage.setItem(KEY, JSON.stringify(list.slice(0, MAX))); } catch (e) { /* see owner() */ }
      paint();
      listeners.forEach(function (fn) { fn(); });
    };

    var post = function (handler, data) {
      var body = new URLSearchParams(data);
      var tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
      if (tokenInput) body.append('__RequestVerificationToken', tokenInput.value);
      return fetch('/wishlist?handler=' + handler, { method: 'POST', body: body, credentials: 'same-origin' })
        .then(function (r) { if (!r.ok) throw new Error('Wishlist ' + r.status); return r.json(); });
    };
    var bump = function (el, css) { el.classList.remove(css); void el.offsetWidth; el.classList.add(css); };

    var paint = function (bumped) {
      Array.prototype.forEach.call(document.querySelectorAll('[data-wish]'), function (btn) {
        var on = has(btn.dataset.wish);
        btn.setAttribute('aria-pressed', on ? 'true' : 'false');
        btn.setAttribute('aria-label', on ? 'Remove ' + btn.dataset.wishName + ' from your wishlist' : 'Save ' + btn.dataset.wishName + ' to your wishlist');
        var label = btn.querySelector('[data-wish-label]');
        if (label) label.textContent = on ? 'Saved to wishlist' : 'Save to wishlist';
      });
      Array.prototype.forEach.call(document.querySelectorAll('[data-wish-count]'), function (el) {
        el.textContent = String(list.length);
        el.hidden = list.length === 0;
        var link = el.closest('.wish-link');
        if (link) link.classList.toggle('has-items', list.length > 0);
        if (bumped) bump(el, 'bump');
      });
    };

    var set = function (slug, on) {
      if (on === has(slug)) return;
      if (on) list.unshift({ s: slug, t: Date.now() });
      else list = list.filter(function (x) { return x.s !== slug; });
      list = list.slice(0, MAX);
      save();
      paint(true);
      if (userId) post('toggle', { bike: slug, saved: on }).catch(function () { /* the browser copy still holds it */ });
    };

    document.addEventListener('click', function (event) {
      var btn = event.target.closest('[data-wish]');
      if (!btn) return;
      var on = !has(btn.dataset.wish);
      set(btn.dataset.wish, on);
      if (on) bump(btn, 'pop');
    });
    window.addEventListener('storage', function (event) {
      if (event.key === KEY) { list = read(); paint(); listeners.forEach(function (fn) { fn(); }); }
    });

    // First visit on this browser as this account: the browser's bikes join the account. After that the account wins.
    var ready = Promise.resolve();
    if (userId) {
      ready = post('merge', { bikes: owner() !== userId ? slugs().join(',') : '' })
        .then(function (saved) {
          list = saved.map(function (x) { return { s: x.slug, t: x.added }; });
          owner(userId);
          save();
        })
        .catch(function () { /* keep showing the browser copy */ });
    } else if (owner()) {
      // Signed out: forget the copy that came from the account, so the next person on this device doesn't see it.
      list = [];
      owner('');
      save();
    }
    paint();

    // The wishlist page.
    var host = document.querySelector('[data-wl-list]');
    if (!host) return;
    var own = host.dataset.wlMode !== 'shared';
    var filter = 'all', sort = 'saved', picked = [];

    var toast = document.querySelector('[data-wl-toast]');
    var toastText = toast.querySelector('[data-wl-toast-text]');
    var undoButton = toast.querySelector('[data-wl-undo]');
    var toastTimer, undo = null;
    var showToast = function (text, onUndo) {
      toastText.textContent = text;
      undo = onUndo || null;
      undoButton.hidden = !undo;
      toast.hidden = false;
      clearTimeout(toastTimer);
      toastTimer = setTimeout(function () { toast.hidden = true; undo = null; }, 6000);
    };
    undoButton.addEventListener('click', function () { if (undo) undo(); toast.hidden = true; undo = null; });

    var ago = function (t) {
      var days = Math.floor((Date.now() - t) / 86400000);
      if (days <= 0) return 'Saved today';
      if (days === 1) return 'Saved yesterday';
      if (days < 30) return 'Saved ' + days + ' days ago';
      return 'Saved ' + new Date(t).toLocaleDateString('en-IN', { day: 'numeric', month: 'short', year: 'numeric' });
    };

    var bar = document.querySelector('[data-wl-comparebar]');
    var go = bar.querySelector('[data-wl-compare-go]');
    var updateBar = function () {
      bar.hidden = picked.length === 0;
      document.body.classList.toggle('has-comparebar', picked.length > 0);
      bar.querySelector('[data-wl-compare-count]').textContent = String(picked.length);
      var ok = picked.length >= 2;
      bar.querySelector('.wl-hint').textContent = ok ? (picked.length === 2 ? 'Add one more, or compare these two' : 'Ready to compare') : 'Pick 2 or 3 to compare';
      go.setAttribute('aria-disabled', ok ? 'false' : 'true');
      // Same order as the canonical compare address, so there is no redirect.
      go.href = ok ? '/compare/' + picked.slice().sort().join('-vs-') : '/compare';
    };

    var apply = function () {
      var grid = host.querySelector('[data-wl-grid]');
      if (!grid) return;
      var price = function (el) { return el.dataset.price ? Number(el.dataset.price) : null; };
      Array.prototype.slice.call(grid.querySelectorAll('[data-wl-item]'))
        .sort(function (a, b) {
          if (sort === 'name') return a.dataset.name.localeCompare(b.dataset.name);
          if (sort === 'price-asc' || sort === 'price-desc') {
            var x = price(a), y = price(b);
            if (x === null) return y === null ? 0 : 1;
            if (y === null) return -1;
            return sort === 'price-asc' ? x - y : y - x;
          }
          return a.dataset.order - b.dataset.order;
        })
        .forEach(function (el) {
          grid.appendChild(el);
          el.hidden = !(filter === 'all' || el.dataset.fuel === filter || el.dataset.type === filter);
        });
      Array.prototype.forEach.call(host.querySelectorAll('[data-wl-filter]'), function (b) {
        b.setAttribute('aria-pressed', b.dataset.wlFilter === filter ? 'true' : 'false');
      });
    };

    var wire = function () {
      var present = function (slug) { return !!host.querySelector('[data-wl-item][data-slug="' + slug + '"]'); };
      picked = picked.filter(present);
      if (!host.querySelector('[data-wl-filter="' + filter + '"]')) filter = 'all';
      var sortSelect = host.querySelector('[data-wl-sort]');
      if (sortSelect) sortSelect.value = sort;
      Array.prototype.forEach.call(host.querySelectorAll('[data-wl-compare]'), function (box) {
        box.checked = picked.indexOf(box.value) >= 0;
        box.closest('[data-wl-item]').classList.toggle('is-picked', box.checked);
      });
      if (own) {
        Array.prototype.forEach.call(host.querySelectorAll('[data-wl-added]'), function (el) {
          var slug = el.closest('[data-wl-item]').dataset.slug;
          var entry = list.filter(function (x) { return x.s === slug; })[0];
          if (entry && entry.t) el.textContent = ago(entry.t);
        });
      }
      apply();
      updateBar();
      paint();
    };

    var load = function () {
      return fetch('/wishlist?handler=items&bikes=' + encodeURIComponent(slugs().join(',')), { credentials: 'same-origin' })
        .then(function (r) { return r.text(); })
        .then(function (html) { host.innerHTML = html; wire(); })
        .catch(function () { host.innerHTML = '<p class="notice">Your saved bikes could not be loaded. Please refresh the page.</p>'; });
    };

    host.addEventListener('click', function (event) {
      var filterButton = event.target.closest('[data-wl-filter]');
      if (filterButton) { filter = filterButton.dataset.wlFilter; apply(); return; }
      var remove = event.target.closest('[data-wl-remove]');
      if (!remove) return;
      var item = remove.closest('[data-wl-item]');
      var slug = item.dataset.slug;
      var entry = list.filter(function (x) { return x.s === slug; })[0];
      item.classList.add('is-leaving');
      setTimeout(function () { set(slug, false); }, 250);
      showToast('Removed ' + item.dataset.name, entry && function () {
        list.push(entry);
        list.sort(function (a, b) { return b.t - a.t; });
        save();
        if (userId) post('toggle', { bike: entry.s, saved: true }).catch(function () {});
      });
    });
    host.addEventListener('change', function (event) {
      if (event.target.matches('[data-wl-sort]')) { sort = event.target.value; apply(); return; }
      var box = event.target.closest('[data-wl-compare]');
      if (!box) return;
      if (box.checked && picked.length >= 3) {
        box.checked = false;
        showToast('You can compare up to 3 bikes at a time.');
        return;
      }
      picked = picked.filter(function (s) { return s !== box.value; });
      if (box.checked) picked.push(box.value);
      box.closest('[data-wl-item]').classList.toggle('is-picked', box.checked);
      updateBar();
    });
    bar.querySelector('[data-wl-compare-clear]').addEventListener('click', function () { picked = []; wire(); });

    var share = document.querySelector('[data-wl-share]');
    if (share) share.addEventListener('click', function () {
      if (!list.length) { showToast('Save a bike first, then share your list.'); return; }
      var url = location.origin + '/wishlist?bikes=' + slugs().join(',');
      var copy = function () {
        if (!navigator.clipboard) { showToast(url); return; }
        navigator.clipboard.writeText(url).then(
          function () { showToast('Link copied. Anyone with it can see this list.'); },
          function () { showToast(url); });
      };
      // Phones get the system share sheet; desktops copy the link, which is what people expect there.
      if (navigator.share && matchMedia('(pointer: coarse)').matches) {
        navigator.share({ title: 'My bike shortlist', url: url })
          .catch(function (error) { if (!error || error.name !== 'AbortError') copy(); });
      } else copy();
    });

    var clear = document.querySelector('[data-wl-clear]');
    var disarmTimer;
    var disarm = function () { clear.classList.remove('is-armed'); clear.textContent = 'Clear all'; };
    if (clear) clear.addEventListener('click', function () {
      if (!list.length) return;
      // Two taps instead of a confirm dialog.
      if (!clear.classList.contains('is-armed')) {
        clear.classList.add('is-armed');
        clear.textContent = 'Tap again to clear';
        clearTimeout(disarmTimer);
        disarmTimer = setTimeout(disarm, 4000);
        return;
      }
      clearTimeout(disarmTimer);
      disarm();
      var before = list.slice();
      list = [];
      save();
      if (userId) before.forEach(function (x) { post('toggle', { bike: x.s, saved: false }).catch(function () {}); });
      showToast('Cleared ' + before.length + (before.length === 1 ? ' bike' : ' bikes'), function () {
        list = before;
        save();
        if (userId) post('merge', { bikes: before.map(function (x) { return x.s; }).join(',') }).catch(function () {});
      });
    });

    var saveAll = document.querySelector('[data-wl-save-all]');
    if (saveAll) saveAll.addEventListener('click', function () {
      var added = 0;
      Array.prototype.forEach.call(host.querySelectorAll('[data-wl-item]'), function (el) {
        if (!has(el.dataset.slug)) { set(el.dataset.slug, true); added++; }
      });
      showToast(added ? 'Saved ' + added + (added === 1 ? ' bike' : ' bikes') + ' to your wishlist.' : 'These bikes are already in your wishlist.');
    });

    ready.then(function () {
      if (own) {
        var shown = Array.prototype.map.call(host.querySelectorAll('[data-wl-item]'), function (el) { return el.dataset.slug; }).join(',');
        if (host.dataset.wlMode === 'browser' || shown !== slugs().join(',')) load(); else wire();
        var reloadTimer;
        listeners.push(function () { clearTimeout(reloadTimer); reloadTimer = setTimeout(load, 150); });
      } else {
        wire();
      }
    });
  })();
})();
