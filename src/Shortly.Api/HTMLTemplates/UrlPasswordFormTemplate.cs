using System.Web;

namespace Shortly.API.HTMLTemplates;

/// <summary>
/// Provides a template generator for rendering a password-protected short URL form.
/// </summary>
/// <remarks>
/// This class is responsible for generating an HTML form that prompts the user
/// to enter a password to access a protected short URL. It includes
/// styling, validation logic, and an optional error message display.
/// </remarks>
public static class UrlPasswordFormTemplate
{
    /// <summary>
    /// Generates the HTML form template for a password-protected short URL.
    /// </summary>
    /// <param name="errorMessage">
    /// An optional error message to be displayed above the form.
    /// If <c>null</c> or empty, no error section will be rendered.
    /// </param>
    /// <param name="token">
    /// The unique token associated with the short URL.
    /// This value is embedded in a hidden form field to ensure secure
    /// verification during submission.
    /// </param>
    /// <returns>
    /// A complete HTML string containing the styled password form, 
    /// including optional error display and client-side validation.
    /// </returns>
    /// <remarks>
    /// The returned HTML includes inline CSS for styling and JavaScript 
    /// for basic password input validation. This template is intended
    /// to be served as a full-page response.
    /// </remarks>
    public static string Generate(string? errorMessage = null, string token = "")
    {
        var error = !string.IsNullOrEmpty(errorMessage)
            ? $@"
              <div class='alert-error' role='alert' aria-live='polite'>
                <span class='alert-icon'>
                  <!-- Heroicons: exclamation-circle -->
                  <svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='currentColor'>
                    <path fill-rule='evenodd' d='M12 2.25c-5.385 0-9.75 4.365-9.75
                      9.75s4.365 9.75 9.75 9.75 9.75-4.365 9.75-9.75S17.385
                      2.25 12 2.25Zm-1.72 6.97a.75.75 0 1 0-1.06 1.06L10.94
                      12l-1.72 1.72a.75.75 0 1 0 1.06 1.06L12 13.06l1.72
                      1.72a.75.75 0 1 0 1.06-1.06L13.06 12l1.72-1.72a.75.75
                      0 1 0-1.06-1.06L12 10.94l-1.72-1.72Z'
                      clip-rule='evenodd'/>
                  </svg>
                </span>
                <span class='alert-text'>{HttpUtility.HtmlEncode(errorMessage)}</span>
              </div>"
            : string.Empty;
 
        return $@"<!DOCTYPE html>
<html lang='en'>
<head>
  <meta charset='UTF-8'>
  <meta name='viewport' content='width=device-width, initial-scale=1.0'>
  <meta name='robots' content='noindex, nofollow'>
  <title>Password Required · Shortly</title>
 
  <!-- Shortly Design System — single-file embed, no CDN -->
  <style>
    /* ── Reset ─────────────────────────────────────────── */
    *, *::before, *::after {{ box-sizing: border-box; margin: 0; padding: 0; }}
    html {{ scrollbar-width: none; -ms-overflow-style: none; }}
    html::-webkit-scrollbar {{ display: none; }}
    a {{ color: inherit; text-decoration: none; }}
    button, input {{ font-family: inherit; }}
 
    /* ── Design tokens (mirrors variables.css) ──────────── */
    :root {{
      --primary-gradient:   linear-gradient(135deg, #667eea, #764ba2);
      --accent-gradient:    linear-gradient(135deg, #1e7bc9, #0891b2);
      --error-gradient:     linear-gradient(135deg, #ef4444, #dc2626);
      --dark-bg:            #0a0a0a;
      --card-bg:            hsla(0, 0%, 100%, 0.05);
      --text-primary:       #fff;
      --text-secondary:     hsla(0, 0%, 100%, 0.7);
      --text-muted:         hsla(0, 0%, 100%, 0.45);
      --border-color:       hsla(0, 0%, 100%, 0.10);
      --border-focus:       #667eea;
      --success-color:      #22c55e;
      --error-color:        #ef4444;
      --shadow-sm:          0 2px  4px  rgba(0,0,0,.10);
      --shadow-md:          0 4px  12px rgba(0,0,0,.20);
      --shadow-lg:          0 8px  24px rgba(0,0,0,.30);
      --shadow-xl:          0 12px 40px rgba(0,0,0,.40);
      --radius-sm:          8px;
      --radius-md:          12px;
      --radius-lg:          16px;
      --radius-xl:          24px;
      --transition-base:    0.3s ease;
      --transition-fast:    0.15s ease;
      --backdrop-blur:      blur(20px);
      --accent-purple:      #667eea;
      --accent-pink:        #f472b6;
      --accent-blue:        #4facfe;
    }}
 
    /* ── Typography (mirrors typography.css) ───────────── */
    body {{
      background:  var(--dark-bg);
      color:       var(--text-primary);
      font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
      line-height: 1.6;
      min-height:  100vh;
      overflow-x:  hidden;
      display:     flex;
      align-items: center;
      justify-content: center;
      padding:     1.25rem;
    }}
 
    /* Animated radial canvas — mirrors typography.css body::before */
    body::before {{
      content:  '';
      position: fixed;
      inset:    0;
      z-index:  -1;
      animation: float 22s ease-in-out infinite;
      background:
        radial-gradient(circle at 18% 82%, rgba(102,126,234,.12) 0, transparent 48%),
        radial-gradient(circle at 82% 18%, rgba(240,147,251,.10) 0, transparent 48%),
        radial-gradient(circle at 50% 50%, rgba(79,172,254,.07)  0, transparent 55%);
    }}
 
    /* Subtle dot-grid overlay for depth */
    body::after {{
      content:  '';
      position: fixed;
      inset:    0;
      z-index:  -1;
      background-image: radial-gradient(circle, hsla(0,0%,100%,.04) 1px, transparent 1px);
      background-size: 28px 28px;
    }}
 
    @keyframes float {{
      0%,100% {{ transform: translate(0)           rotate(0deg);   }}
      33%      {{ transform: translate(28px,-28px)  rotate(120deg); }}
      66%      {{ transform: translate(-20px,20px)  rotate(240deg); }}
    }}
 
    /* ── Card shell (mirrors cards.css .stat-card) ─────── */
    .gate-card {{
      background:      var(--card-bg);
      backdrop-filter: var(--backdrop-blur);
      -webkit-backdrop-filter: var(--backdrop-blur);
      border:          1px solid var(--border-color);
      border-radius:   var(--radius-xl);
      box-shadow:      var(--shadow-xl);
      max-width:       440px;
      width:           100%;
      overflow:        hidden;
      animation:       cardIn .45s cubic-bezier(.22,1,.36,1) both;
      position:        relative;
    }}
 
    /* top accent stripe */
    .gate-card::before {{
      content:  '';
      display:  block;
      height:   2px;
      background: var(--primary-gradient);
    }}
 
    @keyframes cardIn {{
      from {{ opacity:0; transform: translateY(24px) scale(.97); }}
      to   {{ opacity:1; transform: translateY(0)    scale(1);   }}
    }}
 
    /* ── Card inner padding ─────────────────────────────── */
    .gate-inner {{ padding: 2.25rem 2rem 2rem; }}
 
    /* ── Brand / Logo ───────────────────────────────────── */
    .brand {{
      display:         flex;
      align-items:     center;
      gap:             .6rem;
      justify-content: center;
      margin-bottom:   1.75rem;
    }}
 
    .brand-logo {{
      width:  36px;
      height: 36px;
      flex-shrink: 0;
    }}
 
    .brand-name {{
      font-size:   1.45rem;
      font-weight: 700;
      letter-spacing: -.5px;
      background: var(--primary-gradient);
      -webkit-background-clip: text;
      -webkit-text-fill-color: transparent;
      background-clip: text;
    }}
 
    /* ── Lock icon area ─────────────────────────────────── */
    .gate-icon-wrap {{
      display:         flex;
      justify-content: center;
      margin-bottom:   1.25rem;
    }}
 
    .gate-icon {{
      width:  60px;
      height: 60px;
      border-radius: var(--radius-lg);
      background: linear-gradient(135deg,
        hsla(102,126,234,.15) 0%,
        hsla(118,75,162,.10) 100%);
      border:          1px solid hsla(0,0%,100%,.12);
      display:         flex;
      align-items:     center;
      justify-content: center;
      box-shadow:      0 0 0 8px hsla(102,126,234,.06),
                       0 0 0 16px hsla(102,126,234,.03);
    }}
 
    .gate-icon svg {{
      width:  28px;
      height: 28px;
      color:  var(--accent-purple);
    }}
 
    /* ── Heading & subtext ──────────────────────────────── */
    .gate-heading {{
      text-align:  center;
      margin-bottom: 1.75rem;
    }}
 
    .gate-heading h1 {{
      font-size:   1.5rem;
      font-weight: 700;
      color:       var(--text-primary);
      line-height: 1.25;
      margin-bottom: .4rem;
    }}
 
    .gate-heading p {{
      font-size: .9rem;
      color:     var(--text-muted);
      line-height: 1.5;
    }}
 
    /* ── Error alert (mirrors cards.css / semantic tokens) ─ */
    .alert-error {{
      display:       flex;
      align-items:   flex-start;
      gap:           .7rem;
      background:    hsla(0, 68%, 51%, .10);
      border:        1px solid hsla(0, 68%, 51%, .28);
      border-left:   3px solid var(--error-color);
      border-radius: var(--radius-sm);
      padding:       .8rem 1rem;
      margin-bottom: 1.25rem;
      animation:     alertShake .35s ease both;
    }}
 
    @keyframes alertShake {{
      0%,100% {{ transform: translateX(0);    }}
      20%      {{ transform: translateX(-5px); }}
      40%      {{ transform: translateX(5px);  }}
      60%      {{ transform: translateX(-3px); }}
      80%      {{ transform: translateX(3px);  }}
    }}
 
    .alert-icon {{
      flex-shrink: 0;
      width:  18px;
      height: 18px;
      color:  var(--error-color);
      margin-top: 1px;
    }}
 
    .alert-icon svg {{ width: 18px; height: 18px; }}
 
    .alert-text {{
      font-size:   .875rem;
      color:       #fca5a5;
      line-height: 1.45;
    }}
 
    /* ── Form elements ──────────────────────────────────── */
    .form-group {{ margin-bottom: 1.25rem; }}
 
    label {{
      display:       block;
      font-size:     .825rem;
      font-weight:   500;
      color:         var(--text-secondary);
      margin-bottom: .5rem;
      letter-spacing: .01em;
    }}
 
    .input-wrap {{
      position: relative;
      display:  flex;
      align-items: center;
    }}
 
    .input-wrap input[type='password'],
    .input-wrap input[type='text'] {{
      width:         100%;
      padding:       .8rem 2.8rem .8rem 1rem;
      background:    hsla(0,0%,100%,.04);
      border:        1px solid var(--border-color);
      border-radius: var(--radius-md);
      color:         var(--text-primary);
      font-size:     .95rem;
      line-height:   1.5;
      transition:    border-color var(--transition-fast),
                     box-shadow   var(--transition-fast),
                     background   var(--transition-fast);
      outline: none;
    }}
 
    .input-wrap input::placeholder {{ color: var(--text-muted); }}
 
    .input-wrap input:focus {{
      border-color: var(--border-focus);
      background:   hsla(102,126,234,.06);
      box-shadow:   0 0 0 3px hsla(102,126,234,.15);
    }}
 
    /* Show/Hide toggle ─ pure JS, no CDN */
    .toggle-pw {{
      position:   absolute;
      right:      .75rem;
      background: none;
      border:     none;
      cursor:     pointer;
      color:      var(--text-muted);
      padding:    0;
      display:    flex;
      align-items: center;
      transition: color var(--transition-fast);
      line-height: 1;
    }}
 
    .toggle-pw:hover {{ color: var(--text-primary); }}
    .toggle-pw svg   {{ width: 18px; height: 18px; pointer-events: none; }}
 
    /* ── Submit button ──────────────────────────────────── */
    .btn-primary {{
      display:         flex;
      align-items:     center;
      justify-content: center;
      gap:             .5rem;
      width:           100%;
      padding:         .9rem 1.5rem;
      background:      var(--primary-gradient);
      color:           #fff;
      border:          none;
      border-radius:   var(--radius-md);
      font-size:       .95rem;
      font-weight:     600;
      letter-spacing:  .02em;
      cursor:          pointer;
      position:        relative;
      overflow:        hidden;
      transition:      transform var(--transition-fast),
                       box-shadow var(--transition-base),
                       opacity    var(--transition-fast);
      margin-top:      .25rem;
    }}
 
    /* shine sweep on hover */
    .btn-primary::after {{
      content:    '';
      position:   absolute;
      top:        0; left: -80%;
      width:      60%; height: 100%;
      background: linear-gradient(90deg, transparent, hsla(0,0%,100%,.18), transparent);
      transform:  skewX(-20deg);
      transition: left .6s ease;
    }}
 
    .btn-primary:hover {{
      transform:  translateY(-2px);
      box-shadow: 0 8px 24px hsla(102,126,234,.40);
    }}
 
    .btn-primary:hover::after {{ left: 150%; }}
    .btn-primary:active {{ transform: translateY(0); box-shadow: none; }}
 
    /* Loading state */
    .btn-primary.loading {{
      opacity:         .7;
      pointer-events:  none;
    }}
 
    .btn-primary.loading .btn-text  {{ opacity: 0; }}
    .btn-primary.loading .btn-spinner {{ display: block !important; }}
 
    .btn-spinner {{
      display:   none;
      position:  absolute;
      width:  20px; height: 20px;
      border:        2px solid hsla(0,0%,100%,.3);
      border-top-color: #fff;
      border-radius: 50%;
      animation: spin .7s linear infinite;
    }}
 
    @keyframes spin {{
      to {{ transform: rotate(360deg); }}
    }}
 
    /* ── Divider ─────────────────────────────────────────── */
    .divider {{
      display:    flex;
      align-items: center;
      gap:        .75rem;
      color:      var(--text-muted);
      font-size:  .78rem;
      margin:     1.5rem 0 1.25rem;
      letter-spacing: .04em;
      text-transform: uppercase;
    }}
 
    .divider::before, .divider::after {{
      content:    '';
      flex:       1;
      height:     1px;
      background: var(--border-color);
    }}
 
    /* ── Footer note ─────────────────────────────────────── */
    .gate-footer {{
      text-align:    center;
      padding:       1rem 2rem 1.5rem;
      border-top:    1px solid var(--border-color);
      margin-top:    .25rem;
    }}
 
    .gate-footer p {{
      font-size: .78rem;
      color:     var(--text-muted);
    }}
 
    .gate-footer a {{
      color:       var(--accent-purple);
      font-weight: 500;
      transition:  color var(--transition-fast);
    }}
 
    .gate-footer a:hover {{ color: var(--text-primary); }}
 
    /* ── Responsive ─────────────────────────────────────── */
    @media (max-width: 480px) {{
      body {{ padding: .75rem; align-items: flex-start; padding-top: 2rem; }}
 
      .gate-inner {{ padding: 1.75rem 1.25rem 1.5rem; }}
 
      .gate-heading h1 {{ font-size: 1.3rem; }}
 
      .gate-footer {{ padding: .85rem 1.25rem 1.25rem; }}
    }}
  </style>
</head>
<body>
 
  <div class='gate-card'>
 
    <!-- Top accent stripe rendered via ::before on .gate-card -->
 
    <div class='gate-inner'>
 
      <!-- ── Brand ─────────────────────────────────────── -->
      <div class='brand'>
        <!-- Shortly wordmark SVG — replace src with your actual asset if needed -->
        <svg class='brand-logo' viewBox='0 0 36 36' fill='none' xmlns='http://www.w3.org/2000/svg'
             aria-hidden='true'>
          <rect width='36' height='36' rx='10' fill='url(#lg-logo)'/>
          <path d='M11 14.5C11 12.567 12.567 11 14.5 11H22v3h-7.5a.5.5 0 0 0-.5.5v1a.5.5
                   0 0 0 .5.5H22c1.933 0 3.5 1.567 3.5 3.5v1C25.5
                   22.433 23.933 24 22 24h-8v-3h8a.5.5 0 0 0 .5-.5v-1a.5.5
                   0 0 0-.5-.5h-7.5C12.567 19 11 17.433 11 15.5v-1Z'
                fill='white'/>
          <defs>
            <linearGradient id='lg-logo' x1='0' y1='0' x2='36' y2='36' gradientUnits='userSpaceOnUse'>
              <stop stop-color='#667eea'/>
              <stop offset='1' stop-color='#764ba2'/>
            </linearGradient>
          </defs>
        </svg>
        <span class='brand-name'>Shortly</span>
      </div>
 
      <!-- ── Lock icon ─────────────────────────────────── -->
      <div class='gate-icon-wrap'>
        <div class='gate-icon'>
          <svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='currentColor' aria-hidden='true'>
            <path fill-rule='evenodd' d='M12 1.5a5.25 5.25 0 0 0-5.25 5.25v3a3 3 0
                 0 0-3 3v6.75a3 3 0 0 0 3 3h10.5a3 3 0 0 0
                 3-3v-6.75a3 3 0 0 0-3-3v-3A5.25 5.25 0 0 0 12
                 1.5Zm3.75 8.25v-3a3.75 3.75 0 1 0-7.5 0v3h7.5Z'
                  clip-rule='evenodd'/>
          </svg>
        </div>
      </div>
 
      <!-- ── Heading ───────────────────────────────────── -->
      <div class='gate-heading'>
        <h1>Password Required</h1>
        <p>This short link is protected. Enter the password to continue.</p>
      </div>
 
      <!-- ── Error alert (injected by C# when present) ── -->
      {error}
 
      <!-- ── Form ──────────────────────────────────────── -->
      <form id='pw-form' method='post' action='/api/url-redirect/verify' novalidate>
        <input type='hidden' name='token' value='{HttpUtility.HtmlEncode(token)}'>
 
        <div class='form-group'>
          <label for='password'>Password</label>
          <div class='input-wrap'>
            <input type='password'
                   id='password'
                   name='password'
                   autocomplete='current-password'
                   placeholder='Enter the password to continue'
                   required
                   autofocus>
 
            <!-- Show / hide toggle — Heroicons: eye / eye-slash -->
            <button type='button' class='toggle-pw' id='toggle-pw'
                    aria-label='Toggle password visibility'
                    aria-pressed='false'>
              <!-- eye icon (shown when password is hidden) -->
              <svg id='icon-eye' xmlns='http://www.w3.org/2000/svg'
                   viewBox='0 0 24 24' fill='currentColor' aria-hidden='true'>
                <path d='M12 15a3 3 0 1 0 0-6 3 3 0 0 0 0 6Z'/>
                <path fill-rule='evenodd' d='M1.323 11.447C2.811 6.976 7.028
                     4.5 12 4.5c4.97 0 9.188 2.476 10.677 6.947a.75.75 0 0
                     1 0 .506C21.188 16.524 16.97 19 12 19c-4.97 0-9.188-2.476-10.677-6.947a.75.75
                     0 0 1 0-.606ZM12 17.25c3.727 0 7.057-1.952
                     8.704-5.25C19.057 8.702 15.727 6.75 12 6.75
                     8.272 6.75 4.942 8.702 3.296 12c1.646 3.298
                     4.976 5.25 8.704 5.25Z' clip-rule='evenodd'/>
              </svg>
              <!-- eye-slash icon (shown when password is visible) -->
              <svg id='icon-eye-slash' xmlns='http://www.w3.org/2000/svg'
                   viewBox='0 0 24 24' fill='currentColor'
                   aria-hidden='true' style='display:none'>
                <path d='M3.53 2.47a.75.75 0 0 0-1.06 1.06l18 18a.75.75 0 1
                         0 1.06-1.06l-18-18ZM22.676 12.553a11.249 11.249 0 0
                         1-2.631 4.31l-3.099-3.099a5.25 5.25 0 0
                         0-6.71-6.71L7.759 4.577A11.217 11.217 0 0 1
                         12 3.75c4.97 0 9.186 2.476 10.676 6.947.07.2.07.412
                         0 .612ZM4.354 8.585l2.006 2.006A5.25 5.25 0 0 0
                         10.83 16.33l2.013 2.013c-.37.086-.753.131-1.143.131-4.97
                         0-9.186-2.476-10.676-6.947a.75.75 0 0 1
                         0-.612A11.235 11.235 0 0 1 4.354 8.585Z'/>
                <path d='M12.75 12.75a.75.75 0 1 1-1.5 0 .75.75 0 0 1 1.5
                         0ZM7.232 11.47a.75.75 0 1 0 1.06 1.06l-1.06-1.06Z'/>
              </svg>
            </button>
          </div>
        </div>
 
        <button type='submit' class='btn-primary' id='submit-btn'>
          <span class='btn-text'>Unlock Link</span>
          <div class='btn-spinner' aria-hidden='true'></div>
        </button>
      </form>
 
    </div><!-- /gate-inner -->
 
    <!-- ── Footer ─────────────────────────────────────── -->
    <div class='gate-footer'>
      <p>Powered by <a href='/' target='_blank' rel='noopener'>Shortly</a> · Secure Link Sharing</p>
    </div>
 
  </div><!-- /gate-card -->
 
  <script>
    (function () {{
      /* ── Show / hide password ─────────────────────── */
      const input    = document.getElementById('password');
      const toggle   = document.getElementById('toggle-pw');
      const iconEye  = document.getElementById('icon-eye');
      const iconSlash= document.getElementById('icon-eye-slash');
 
      toggle.addEventListener('click', function () {{
        const isHidden = input.type === 'password';
        input.type = isHidden ? 'text' : 'password';
        iconEye  .style.display = isHidden ? 'none'  : '';
        iconSlash.style.display = isHidden ? ''      : 'none';
        toggle.setAttribute('aria-pressed', String(isHidden));
      }});
 
      /* ── Form submit — validation + loading state ─── */
      const form      = document.getElementById('pw-form');
      const submitBtn = document.getElementById('submit-btn');
 
      form.addEventListener('submit', function (e) {{
        if (!input.value.trim()) {{
          e.preventDefault();
          input.focus();
          input.style.borderColor = 'var(--error-color)';
          input.style.boxShadow   = '0 0 0 3px hsla(0,68%,51%,.20)';
          setTimeout(function () {{
            input.style.borderColor = '';
            input.style.boxShadow   = '';
          }}, 1800);
          return;
        }}
        /* Show loading state */
        submitBtn.classList.add('loading');
        submitBtn.setAttribute('aria-disabled', 'true');
      }});
 
      /* ── Auto-focus ───────────────────────────────── */
      input.focus();
    }})();
  </script>
</body>
</html>";
    }
}