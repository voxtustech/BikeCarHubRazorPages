// Home page enhancements. The page works without this file; site.js handles the finder and the EMI sums.
(function () {
  'use strict';

  // Mobile menu
  var menuButton = document.querySelector('[data-menu-button]');
  var menu = document.getElementById('top-nav');
  if (menuButton && menu) {
    menuButton.addEventListener('click', function () {
      var open = menu.classList.toggle('open');
      menuButton.setAttribute('aria-expanded', open ? 'true' : 'false');
    });
  }

  // Shadow under the header once the page has scrolled.
  var top = document.querySelector('[data-top]');
  if (top) {
    var onScroll = function () { top.classList.toggle('is-stuck', window.scrollY > 8); };
    window.addEventListener('scroll', onScroll, { passive: true });
    onScroll();
  }

  // Featured showcase: one picture at a time, changed by the dots or every six seconds.
  var slides = Array.prototype.slice.call(document.querySelectorAll('[data-hero-slide]'));
  var dots = Array.prototype.slice.call(document.querySelectorAll('[data-hero-dot]'));
  if (slides.length > 1) {
    var current = 0;
    var timer = null;
    var show = function (index) {
      current = (index + slides.length) % slides.length;
      slides.forEach(function (slide, i) { slide.classList.toggle('is-active', i === current); });
      dots.forEach(function (dot, i) { dot.classList.toggle('is-active', i === current); });
    };
    var restart = function () {
      if (timer) clearInterval(timer);
      if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) return;
      timer = setInterval(function () { show(current + 1); }, 6000);
    };
    dots.forEach(function (dot, i) { dot.addEventListener('click', function () { show(i); restart(); }); });
    restart();
  }

  // Arrow buttons for the horizontal rails.
  Array.prototype.forEach.call(document.querySelectorAll('[data-rail-buttons]'), function (group) {
    var rail = document.getElementById(group.getAttribute('data-rail-buttons'));
    if (!rail) return;
    var step = function (direction) {
      rail.scrollBy({ left: direction * Math.max(280, rail.clientWidth * 0.8), behavior: 'smooth' });
    };
    group.querySelector('[data-rail-prev]').addEventListener('click', function () { step(-1); });
    group.querySelector('[data-rail-next]').addEventListener('click', function () { step(1); });
  });

  // EMI sliders: show the chosen amount and rate beside their labels.
  var emi = document.querySelector('[data-emi-form]');
  if (emi) {
    var amountOut = emi.querySelector('[data-emi-amount]');
    var rateOut = emi.querySelector('[data-emi-rate]');
    var label = function () {
      if (amountOut) amountOut.textContent = '₹' + Number(emi.elements.amount.value).toLocaleString('en-IN');
      if (rateOut) rateOut.textContent = Number(emi.elements.rate.value) + '%';
    };
    emi.addEventListener('input', label);
    label();
  }
})();
